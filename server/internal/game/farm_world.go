package game

import (
	"encoding/json"
	"math"

	"github.com/deathborn/server/internal/db"
	"github.com/deathborn/server/internal/skills"
)

// FarmActionResult is returned after a homestead farm tool/use.
type FarmActionResult struct {
	House        HouseState
	Inventory    []InventoryItem
	Message      string
	GrantedXP    int64
	Skill        string
	Xp           int64
	Level        int
	LeveledUp    bool
	TotalXp      int64
	PlayerAction string
	DirX, DirY   float64
}

func (p *housePlot) markFarmDirty() {
	p.farmDirty = true
}

func (p *housePlot) farmJSON() []byte {
	raw, err := json.Marshal(p.farmPersist())
	if err != nil {
		return []byte("{}")
	}
	return raw
}

func decodeFarm(raw json.RawMessage) FarmPersist {
	if len(raw) == 0 {
		return FarmPersist{}
	}
	var data FarmPersist
	if json.Unmarshal(raw, &data) != nil {
		return FarmPersist{}
	}
	return data
}

func (w *World) TickFarm(dt float64) bool {
	if w.housing == nil {
		return false
	}
	now := farmNow()
	changed := false
	for _, p := range w.housing.byID {
		if p.tickFarm(dt, now) {
			p.markFarmDirty()
			changed = true
		}
	}
	if changed {
		w.houseRev++
	}
	return changed
}

func (w *World) TakeDirtyFarms() []db.HouseFarmSave {
	w.mu.Lock()
	defer w.mu.Unlock()
	if w.housing == nil {
		return nil
	}
	var out []db.HouseFarmSave
	for _, p := range w.housing.byID {
		if !p.farmDirty {
			continue
		}
		p.farmDirty = false
		out = append(out, db.HouseFarmSave{HouseID: p.id, Farm: p.farmJSON()})
	}
	return out
}

func (w *World) plotForFarmer(characterID int64) *housePlot {
	if w.housing == nil {
		return nil
	}
	if p := w.housing.ByCharacter(characterID); p != nil {
		return p
	}
	owner, ok := w.players[characterID]
	if !ok {
		return nil
	}
	for _, it := range owner.inventory {
		if it.ItemID == db.ItemHouseKey && it.HouseID > 0 {
			return w.housing.byID[it.HouseID]
		}
	}
	return nil
}

func faceDir(fromX, fromY, toX, toY float64) (float64, float64) {
	dx := toX - fromX
	dy := toY - fromY
	if dx == 0 && dy == 0 {
		return 0, 1
	}
	if math.Abs(dx) >= math.Abs(dy) {
		if dx < 0 {
			return -1, 0
		}
		return 1, 0
	}
	if dy < 0 {
		return 0, -1
	}
	return 0, 1
}

func (w *World) ApplyFarmAction(characterID int64, action string, tileX, tileY int, itemID string, slot int, animalID int64) (FarmActionResult, string, bool) {
	w.mu.Lock()
	defer w.mu.Unlock()

	player := w.players[characterID]
	if player == nil || player.dead {
		return FarmActionResult{}, "Cannot farm right now.", false
	}
	if player.insideHouseID > 0 {
		return FarmActionResult{}, "Farm outside, in the homestead yard.", false
	}
	plot := w.plotForFarmer(characterID)
	if plot == nil {
		return FarmActionResult{}, "You need a homestead to farm.", false
	}

	now := farmNow()
	slots := itemsToSlots(player.inventory)
	cx, cy := tileCenter(tileX, tileY)
	if animalID <= 0 && (action == "till" || action == "water" || action == "plant" || action == "harvest") {
		if math.Hypot(player.x-cx, player.y-cy) > FarmActionRange {
			return FarmActionResult{}, "Move closer to that soil.", false
		}
		if !plot.tileFarmable(tileX, tileY) {
			return FarmActionResult{}, "Till soil in your fenced yard, not on the cottage.", false
		}
		if w.terrain != nil && !w.CanWalk(cx, cy) {
			return FarmActionResult{}, "That ground cannot be farmed.", false
		}
	}

	dirX, dirY := faceDir(player.x, player.y, cx, cy)
	result := FarmActionResult{DirX: dirX, DirY: dirY}

	switch action {
	case "till":
		idx := plot.findCrop(tileX, tileY)
		if idx >= 0 {
			t := plot.crops[idx]
			if t.crop != "" && t.ready() {
				msg, xp, ok := harvestTile(plot, idx, &slots, now)
				if !ok {
					return FarmActionResult{}, msg, false
				}
				result.Message = msg
				result.GrantedXP = xp
				result.Skill = skills.Farming
				result.PlayerAction = "hoe"
				break
			}
			return FarmActionResult{}, "That soil is already tilled.", false
		}
		if len(plot.crops) >= FarmMaxTiles {
			return FarmActionResult{}, "The yard is fully tilled.", false
		}
		plot.crops = append(plot.crops, farmTile{tx: tileX, ty: tileY})
		plot.markFarmDirty()
		result.Message = "You till the soil."
		result.PlayerAction = "hoe"
		result.GrantedXP = 4
		result.Skill = skills.Farming

	case "water":
		idx := plot.findCrop(tileX, tileY)
		if idx < 0 {
			return FarmActionResult{}, "Till the soil before watering.", false
		}
		plot.crops[idx].wateredUntil = now + FarmWaterDuration
		plot.markFarmDirty()
		result.Message = "You water the soil."
		result.PlayerAction = "water"

	case "plant":
		def, ok := cropDefBySeed(itemID)
		if !ok {
			return FarmActionResult{}, "Those are not seeds.", false
		}
		idx := plot.findCrop(tileX, tileY)
		if idx < 0 {
			return FarmActionResult{}, "Till the soil first.", false
		}
		if plot.crops[idx].crop != "" {
			return FarmActionResult{}, "Something is already growing there.", false
		}
		next, took := takeItemsFromSlots(slots, def.seedItem, 1, slot)
		if !took {
			return FarmActionResult{}, "You need those seeds.", false
		}
		slots = next
		plot.crops[idx].crop = def.id
		plot.crops[idx].stage = 0
		plot.crops[idx].growAccum = 0
		plot.markFarmDirty()
		result.Message = "You plant " + def.name + " seeds."
		result.PlayerAction = "hoe"
		result.GrantedXP = 8
		result.Skill = skills.Farming

	case "harvest":
		idx := plot.findCrop(tileX, tileY)
		if idx < 0 {
			return FarmActionResult{}, "Nothing to harvest there.", false
		}
		msg, xp, ok := harvestTile(plot, idx, &slots, now)
		if !ok {
			return FarmActionResult{}, msg, false
		}
		result.Message = msg
		result.GrantedXP = xp
		result.Skill = skills.Farming
		result.PlayerAction = "hoe"

	case "place_animal":
		def, ok := animalDefByItem(itemID)
		if !ok {
			return FarmActionResult{}, "That is not a farm animal.", false
		}
		if len(plot.animals) >= FarmMaxAnimals {
			return FarmActionResult{}, "The homestead cannot hold more animals.", false
		}
		if !plot.inPlot(player.x, player.y) {
			return FarmActionResult{}, "Place animals in your homestead yard.", false
		}
		ax, ay := player.x+dirX*12, player.y+dirY*12
		if !plot.inPlot(ax, ay) || overlapsHouseBody(ax, ay, plot.centerX, plot.centerY) {
			ax, ay = player.x, player.y
		}
		next, took := takeItemsFromSlots(slots, def.item, 1, slot)
		if !took {
			return FarmActionResult{}, "You do not have that animal.", false
		}
		slots = next
		plot.nextAnimalID++
		id := plot.nextAnimalID
		productAt := now + FarmProductReadyIn
		plot.animals = append(plot.animals, farmAnimal{
			id: id, kind: def.id, x: ax, y: ay, productAt: productAt,
		})
		plot.markFarmDirty()
		result.Message = "A " + def.name + " settles into the yard."
		result.PlayerAction = "hoe"

	case "feed":
		idx := plot.nearestAnimal(player.x, player.y, FarmActionRange)
		if animalID > 0 {
			idx = plot.findAnimal(animalID)
		}
		if idx < 0 {
			return FarmActionResult{}, "No animal close enough to feed.", false
		}
		next, took := takeItemsFromSlots(slots, "animal_feed", 1, slot)
		if !took {
			return FarmActionResult{}, "You need animal feed.", false
		}
		slots = next
		a := &plot.animals[idx]
		a.feedUntil = now + FarmFeedDuration
		if def, ok := animalDefs[a.kind]; ok && def.product != "" && !a.productReady {
			readyIn := FarmProductReadyIn * 0.55
			if a.productAt > now+readyIn {
				a.productAt = now + readyIn
			}
		}
		plot.markFarmDirty()
		result.Message = "You toss out some feed."
		result.GrantedXP = 6
		result.Skill = skills.Farming
		result.PlayerAction = "hoe"
		dirX, dirY = faceDir(player.x, player.y, plot.animals[idx].x, plot.animals[idx].y)
		result.DirX, result.DirY = dirX, dirY

	case "collect":
		idx := plot.findAnimal(animalID)
		if idx < 0 {
			idx = plot.nearestAnimal(player.x, player.y, FarmActionRange)
		}
		if idx < 0 {
			return FarmActionResult{}, "No animal close enough.", false
		}
		a := &plot.animals[idx]
		if math.Hypot(player.x-a.x, player.y-a.y) > FarmActionRange {
			return FarmActionResult{}, "Move closer to the animal.", false
		}
		def, ok := animalDefs[a.kind]
		if !ok || def.product == "" {
			return FarmActionResult{}, "This animal has nothing to collect.", false
		}
		if !a.productReady {
			return FarmActionResult{}, def.name + " is not ready yet.", false
		}
		loot := InventoryItem{ItemID: def.product, Count: 1}
		if !inventoryHasRoom(slots, loot) {
			return FarmActionResult{}, "Inventory is full.", false
		}
		slots = addItemToSlots(slots, loot)
		a.productReady = false
		a.productAt = now + FarmProductReadyIn
		plot.markFarmDirty()
		result.Message = "You collect " + def.productName + "."
		result.GrantedXP = def.xp
		result.Skill = skills.Farming
		result.PlayerAction = "hoe"
		dirX, dirY = faceDir(player.x, player.y, a.x, a.y)
		result.DirX, result.DirY = dirX, dirY

	case "pet":
		idx := plot.findAnimal(animalID)
		if idx < 0 {
			idx = plot.nearestAnimal(player.x, player.y, FarmActionRange)
		}
		if idx < 0 {
			return FarmActionResult{}, "No animal close enough.", false
		}
		a := plot.animals[idx]
		if math.Hypot(player.x-a.x, player.y-a.y) > FarmActionRange {
			return FarmActionResult{}, "Move closer to the animal.", false
		}
		def := animalDefs[a.kind]
		result.Message = "You pet the " + def.name + "."
		result.GrantedXP = 4
		result.Skill = skills.Farming
		result.PlayerAction = "hoe"
		dirX, dirY = faceDir(player.x, player.y, a.x, a.y)
		result.DirX, result.DirY = dirX, dirY

	default:
		return FarmActionResult{}, "Unknown farm action.", false
	}

	player.inventory = slotsToItems(slots)
	if result.GrantedXP > 0 {
		if now := farmNow(); w.canGrantFarmXP(player, now) {
			player.lastSkillInteract = now
			oldLevel := player.skills.Level(result.Skill)
			newXp, level, leveled := player.skills.Grant(result.Skill, result.GrantedXP)
			player.totalXp = player.skills.TotalXP()
			result.Xp = newXp
			result.Level = level
			result.LeveledUp = leveled || level > oldLevel
			result.TotalXp = player.totalXp
		} else {
			result.GrantedXP = 0
		}
	}
	w.houseRev++
	result.House = plot.state()
	result.Inventory = append([]InventoryItem(nil), player.inventory...)
	return result, "", true
}

func harvestTile(plot *housePlot, idx int, slots *[InventorySlotCount]InventoryItem, _ float64) (string, int64, bool) {
	t := plot.crops[idx]
	if t.crop == "" || !t.ready() {
		return "It is not ready to harvest.", 0, false
	}
	def, ok := cropDefs[t.crop]
	if !ok {
		return "Unknown crop.", 0, false
	}
	loot := InventoryItem{ItemID: def.produceItem, Count: def.yield}
	seedBonus := InventoryItem{ItemID: def.seedItem, Count: 1}
	if !inventoryHasRoom(*slots, loot) {
		return "Inventory is full.", 0, false
	}
	*slots = addItemToSlots(*slots, loot)
	if inventoryHasRoom(*slots, seedBonus) {
		*slots = addItemToSlots(*slots, seedBonus)
	}
	if def.regrowStage >= 0 {
		plot.crops[idx].stage = def.regrowStage
		plot.crops[idx].growAccum = 0
		plot.crops[idx].wateredUntil = 0
	} else {
		plot.crops[idx].crop = ""
		plot.crops[idx].stage = 0
		plot.crops[idx].growAccum = 0
	}
	plot.markFarmDirty()
	return "You harvest " + def.name + ".", def.xp, true
}

func (w *World) canGrantFarmXP(p *player, now float64) bool {
	if p.skills == nil {
		p.skills = skills.NewSet()
	}
	if now-p.lastSkillInteract < 0.35 {
		return false
	}
	return true
}
