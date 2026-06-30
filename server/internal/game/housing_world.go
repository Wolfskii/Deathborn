package game

import "github.com/deathborn/server/internal/db"

func (w *World) LoadHouses(houses []db.House) {
	if w.housing == nil {
		w.housing = NewHousingIndex()
	}
	w.housing.LoadFromDB(houses)
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
	return p.state(), true
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
	p := w.housing.ByCharacter(characterID)
	return p.state(), "", true
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
