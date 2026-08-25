package game

import (
	"math"

	"github.com/deathborn/server/internal/skills"
)

// FishActionResult is returned after start / catch / cancel.
type FishActionResult struct {
	Inventory    []InventoryItem
	ItemID       string
	ItemName     string
	BaitUsed     bool
	GrantedXP    int64
	Skill        string
	Xp           int64
	Level        int
	LeveledUp    bool
	TotalXp      int64
	PlayerAction string
	DirX, DirY   float64
}

func (w *World) ApplyFishAction(playerID int64, action string, tileX, tileY int) (FishActionResult, string, bool) {
	w.mu.Lock()
	defer w.mu.Unlock()

	p, ok := w.players[playerID]
	if !ok || p.dead {
		return FishActionResult{}, "You cannot fish right now.", false
	}
	if p.insideHouseID > 0 {
		return FishActionResult{}, "Fish from the shore, not indoors.", false
	}

	now := farmNow()
	dirX, dirY := faceDir(p.x, p.y, (float64(tileX)+0.5)*FishTileSize, (float64(tileY)+0.5)*FishTileSize)

	switch action {
	case "start":
		if msg, ok := w.validateFishingCast(p, tileX, tileY); !ok {
			return FishActionResult{}, msg, false
		}
		p.fishing = true
		p.fishingTx, p.fishingTy = tileX, tileY
		p.fishingAt = now
		return FishActionResult{PlayerAction: "fish_wait", DirX: dirX, DirY: dirY}, "", true

	case "cancel":
		p.fishing = false
		return FishActionResult{PlayerAction: "fish_stop", DirX: dirX, DirY: dirY}, "", true

	case "catch":
		if !p.fishing || p.fishingTx != tileX || p.fishingTy != tileY {
			return FishActionResult{}, "You need to cast first.", false
		}
		if now-p.fishingAt < FishMinWaitSec {
			return FishActionResult{}, "The line is not ready yet.", false
		}
		if msg, ok := w.validateFishingCast(p, tileX, tileY); !ok {
			p.fishing = false
			return FishActionResult{}, msg, false
		}

		slots := itemsToSlots(p.inventory)
		if !inventoryContains(slots, "fishing_rod") {
			p.fishing = false
			return FishActionResult{}, "You need a fishing rod.", false
		}

		sea := w.terrain != nil && w.terrain.IsSeaTile(tileX, tileY)
		baited := inventoryContains(slots, "worm_bait")
		level := 1
		if p.skills != nil {
			level = p.skills.Level(skills.Fishing)
		}
		def := rollFish(level, sea, baited)
		loot := InventoryItem{ItemID: def.id, Count: 1}
		if !inventoryHasRoom(slots, loot) {
			p.fishing = false
			return FishActionResult{PlayerAction: "fish_stop"}, "Your bags are full.", false
		}
		if baited {
			slots, baited = takeItemsFromSlots(slots, "worm_bait", 1, -1)
		}
		slots = addItemToSlots(slots, loot)
		p.inventory = slotsToItems(slots)
		p.fishing = false

		result := FishActionResult{
			Inventory:    append([]InventoryItem(nil), p.inventory...),
			ItemID:       def.id,
			ItemName:     def.name,
			BaitUsed:     baited,
			GrantedXP:    def.xp,
			Skill:        skills.Fishing,
			PlayerAction: "fish_catch",
			DirX:         dirX,
			DirY:         dirY,
		}
		if now-p.lastSkillInteract >= 1.5 {
			p.lastSkillInteract = now
			if p.skills == nil {
				p.skills = skills.NewSet()
			}
			newXp, lvl, leveled := p.skills.Grant(skills.Fishing, def.xp)
			p.totalXp = p.skills.TotalXP()
			result.Xp = newXp
			result.Level = lvl
			result.LeveledUp = leveled
			result.TotalXp = p.totalXp
		} else {
			result.GrantedXP = 0
		}
		return result, "", true
	}

	return FishActionResult{}, "Unknown fishing action.", false
}

func (w *World) validateFishingCast(p *player, tileX, tileY int) (string, bool) {
	if w.terrain == nil {
		return "There is no water here.", false
	}
	if !w.terrain.IsWaterTile(tileX, tileY) {
		return "Cast into the water.", false
	}
	ptx, pty := w.terrain.TileCoords(p.x, p.y)
	if !w.terrain.IsLandTile(ptx, pty) {
		return "Stand on the shore to fish.", false
	}
	dx := tileX - ptx
	if dx < 0 {
		dx = -dx
	}
	dy := tileY - pty
	if dy < 0 {
		dy = -dy
	}
	if dx > FishMaxChebyshev || dy > FishMaxChebyshev {
		return "Move closer to the water.", false
	}
	cx := (float64(tileX) + 0.5) * FishTileSize
	cy := (float64(tileY) + 0.5) * FishTileSize
	if math.Hypot(p.x-cx, p.y-cy) > FishActionRange {
		return "Move closer to the water.", false
	}
	if !inventoryContains(itemsToSlots(p.inventory), "fishing_rod") {
		return "You need a fishing rod.", false
	}
	return "", true
}

func inventoryContains(slots [InventorySlotCount]InventoryItem, itemID string) bool {
	for i := 0; i < InventorySlotCount; i++ {
		if slots[i].ItemID == itemID && slots[i].Count > 0 {
			return true
		}
	}
	return false
}

func InventoryHasItem(items []InventoryItem, itemID string) bool {
	return inventoryContains(itemsToSlots(items), itemID)
}
