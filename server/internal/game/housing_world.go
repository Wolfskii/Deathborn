package game

import (
	"math"

	"github.com/deathborn/server/internal/db"
)

func (w *World) LoadHouses(houses []db.House) {
	if w.housing == nil {
		w.housing = NewHousingIndex()
	}
	w.housing.LoadFromDB(houses)
	w.houseRev++
}

func (w *World) HouseRevision() uint64 {
	return w.houseRev
}

func (w *World) HouseSnapshot() []HouseState {
	if w.housing == nil {
		return nil
	}
	return w.housing.Snapshot()
}

func (w *World) BuildHouse(characterID int64, ownerName string, x, y float64) (HouseState, string, bool) {
	w.mu.Lock()
	defer w.mu.Unlock()
	if w.housing == nil {
		w.housing = NewHousingIndex()
	}
	if p, ok := w.players[characterID]; ok && playerHasHouseKey(p.inventory) {
		return HouseState{}, "You already carry a homestead key.", false
	}
	if msg := w.housing.CanBuildAt(w, characterID, x, y); msg != "" {
		return HouseState{}, msg, false
	}
	// ID assigned after DB insert in net layer; temp id for in-memory until saved.
	plot := &housePlot{
		characterID: characterID, ownerName: ownerName, centerX: x, centerY: y,
	}
	return plot.state(), "", true
}

func (w *World) RegisterHouse(row db.House) HouseState {
	w.mu.Lock()
	defer w.mu.Unlock()
	plot := dbHouseToPlot(row)
	if w.housing == nil {
		w.housing = NewHousingIndex()
	}
	w.housing.Add(plot)
	w.houseRev++
	return plot.state()
}

func (w *World) RemoveHouseByCharacter(characterID int64) (HouseState, bool) {
	w.mu.Lock()
	defer w.mu.Unlock()
	if w.housing == nil {
		return HouseState{}, false
	}
	p := w.housing.RemoveByCharacter(characterID)
	if p == nil {
		return HouseState{}, false
	}
	w.ejectPlayersFromHouse(p)
	w.houseRev++
	return p.state(), true
}

// DestroyPlayerHouse removes the character's homestead (or key-linked plot), ejects
// anyone inside, and strips matching house keys from the owner's inventory.
func (w *World) DestroyPlayerHouse(characterID int64) (HouseState, []InventoryItem, bool) {
	w.mu.Lock()
	defer w.mu.Unlock()
	if w.housing == nil {
		return HouseState{}, nil, false
	}

	p := w.housing.ByCharacter(characterID)
	if p == nil {
		if owner, ok := w.players[characterID]; ok {
			for _, it := range owner.inventory {
				if it.ItemID == db.ItemHouseKey && it.HouseID > 0 {
					p = w.housing.byID[it.HouseID]
					break
				}
			}
		}
	}
	if p == nil {
		return HouseState{}, nil, false
	}

	removed := w.housing.RemoveByID(p.id)
	if removed == nil {
		return HouseState{}, nil, false
	}
	w.ejectPlayersFromHouse(removed)
	w.houseRev++

	var inv []InventoryItem
	if owner, ok := w.players[characterID]; ok {
		owner.inventory = stripHouseKeys(owner.inventory, removed.id)
		inv = append([]InventoryItem(nil), owner.inventory...)
	}
	return removed.state(), inv, true
}

func (w *World) ejectPlayersFromHouse(plot *housePlot) {
	if plot == nil {
		return
	}
	for _, pl := range w.players {
		if pl.insideHouseID != plot.id {
			continue
		}
		pl.insideHouseID = 0
		pl.x, pl.y = HouseExteriorSpawn(plot.centerX, plot.centerY)
		pl.lockHouseTransition()
	}
}

func stripHouseKeys(items []InventoryItem, houseID int64) []InventoryItem {
	out := make([]InventoryItem, 0, len(items))
	for _, it := range items {
		if it.ItemID == db.ItemHouseKey && (houseID <= 0 || it.HouseID == houseID) {
			continue
		}
		out = append(out, it)
	}
	return out
}

func (w *World) PlaceFurniture(characterID int64, item FurnitureItem) (HouseState, string, bool) {
	w.mu.Lock()
	defer w.mu.Unlock()
	if w.housing == nil {
		return HouseState{}, "You do not have a house.", false
	}
	if msg := w.housing.CanPlaceFurniture(characterID, item); msg != "" {
		return HouseState{}, msg, false
	}
	w.housing.AddFurniture(characterID, item)
	w.houseRev++
	p := w.housing.ByCharacter(characterID)
	return p.state(), "", true
}

func (w *World) RemoveHouseByID(houseID int64) (HouseState, bool) {
	w.mu.Lock()
	defer w.mu.Unlock()
	if w.housing == nil {
		return HouseState{}, false
	}
	p := w.housing.RemoveByID(houseID)
	if p == nil {
		return HouseState{}, false
	}
	w.ejectPlayersFromHouse(p)
	w.houseRev++
	return p.state(), true
}

func (w *World) TransferHouse(houseID, newCharacterID int64, newOwnerName string) (HouseState, bool) {
	w.mu.Lock()
	defer w.mu.Unlock()
	if w.housing == nil {
		return HouseState{}, false
	}
	p := w.housing.TransferOwnership(houseID, newCharacterID, newOwnerName)
	if p == nil {
		return HouseState{}, false
	}
	w.houseRev++
	return p.state(), true
}

func (w *World) FurnitureForSave(characterID int64) ([]FurnitureItem, bool) {
	w.mu.RLock()
	defer w.mu.RUnlock()
	p := w.housing.ByCharacter(characterID)
	if p == nil {
		return nil, false
	}
	return append([]FurnitureItem(nil), p.furniture...), true
}

func (w *World) IsInSafeHaven(x, y float64) bool {
	w.mu.RLock()
	defer w.mu.RUnlock()
	if w.housing != nil && w.housing.Contains(x, y) {
		return true
	}
	if w.zones != nil && w.zones.InSafeArea(x, y) {
		return true
	}
	return false
}

func (w *World) HousingBlocksMonsters(x, y float64) bool {
	w.mu.RLock()
	defer w.mu.RUnlock()
	return w.housing != nil && w.housing.Contains(x, y)
}

func tryAutoEnterHouse(p *player, housing *HousingIndex) {
	if p.insideHouseID > 0 || p.dead || !canAutoHouseTransition(p) {
		return
	}
	// Approach the exterior door from the south (walk north into the doorway).
	if p.dirY >= -0.08 {
		return
	}
	if p.dirX*p.dirX+p.dirY*p.dirY < 0.01 {
		return
	}
	var best *housePlot
	bestDist := math.MaxFloat64
	for _, plot := range housing.byID {
		near := NearHouseDoor(p.x, p.y, plot.centerX, plot.centerY) ||
			inDoorApproach(p.x, p.y, plot.centerX, plot.centerY)
		if !near {
			continue
		}
		dx, dy := HouseDoorPosition(plot.centerX, plot.centerY)
		d := math.Hypot(p.x-dx, p.y-dy)
		if d < bestDist {
			bestDist = d
			best = plot
		}
	}
	if best == nil {
		return
	}
	p.insideHouseID = best.id
	p.x, p.y = HouseInteriorSpawn(best.centerX, best.centerY)
	p.lockHouseTransition()
}

func tryAutoExitHouse(p *player, plot *housePlot) {
	if p.dead || !canAutoHouseTransition(p) || p.dirY < 0.12 {
		return
	}
	if !NearInteriorExit(p.x, p.y, plot.centerX, plot.centerY) {
		return
	}
	p.insideHouseID = 0
	p.x, p.y = HouseExteriorSpawn(plot.centerX, plot.centerY)
	p.lockHouseTransition()
}

func (w *World) EnterHouse(characterID, houseID int64) (string, bool) {
	w.mu.Lock()
	defer w.mu.Unlock()
	p := w.players[characterID]
	if p == nil || p.dead {
		return "Cannot enter right now.", false
	}
	if w.housing == nil {
		return "No house here.", false
	}
	plot := w.housing.byID[houseID]
	if plot == nil {
		return "No house here.", false
	}
	if p.insideHouseID > 0 {
		return "You are already inside.", false
	}
	if !NearHouseDoor(p.x, p.y, plot.centerX, plot.centerY) &&
		!inDoorApproach(p.x, p.y, plot.centerX, plot.centerY) {
		return "Move closer to the door.", false
	}
	p.insideHouseID = houseID
	p.x, p.y = HouseInteriorSpawn(plot.centerX, plot.centerY)
	p.lockHouseTransition()
	return "", true
}

func (w *World) ExitHouse(characterID int64) (string, bool) {
	w.mu.Lock()
	defer w.mu.Unlock()
	p := w.players[characterID]
	if p == nil || p.dead {
		return "Cannot exit right now.", false
	}
	if p.insideHouseID <= 0 || w.housing == nil {
		return "You are not inside a house.", false
	}
	plot := w.housing.byID[p.insideHouseID]
	if plot == nil {
		p.insideHouseID = 0
		return "You step outside.", true
	}
	if !NearInteriorExit(p.x, p.y, plot.centerX, plot.centerY) {
		return "Move to the door to exit.", false
	}
	p.insideHouseID = 0
	p.x, p.y = HouseExteriorSpawn(plot.centerX, plot.centerY)
	p.lockHouseTransition()
	return "", true
}
