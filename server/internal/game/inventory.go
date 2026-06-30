package game

import "github.com/deathborn/server/internal/db"

const PickupRange = 48.0

// InventoryItem is one stack carried by a player.
type InventoryItem struct {
	ItemID  string `json:"itemId"`
	Count   int    `json:"count"`
	HouseID int64  `json:"houseId,omitempty"`
}

func inventoryFromDB(items []db.InventoryItem) []InventoryItem {
	out := make([]InventoryItem, 0, len(items))
	for _, it := range items {
		out = append(out, InventoryItem{ItemID: it.ItemID, Count: it.Count, HouseID: it.HouseID})
	}
	return out
}

func inventoryToDB(items []InventoryItem) []db.InventoryItem {
	out := make([]db.InventoryItem, 0, len(items))
	for _, it := range items {
		out = append(out, db.InventoryItem{ItemID: it.ItemID, Count: it.Count, HouseID: it.HouseID})
	}
	return out
}

func InventoryFromDB(items []db.InventoryItem) []InventoryItem {
	return inventoryFromDB(items)
}

func InventoryToDB(items []InventoryItem) []db.InventoryItem {
	return inventoryToDB(items)
}

func (w *World) SetPlayerInventory(id int64, items []InventoryItem) {
	w.mu.Lock()
	defer w.mu.Unlock()
	if p, ok := w.players[id]; ok {
		p.inventory = append([]InventoryItem(nil), items...)
	}
}

func (w *World) PlayerInventory(id int64) ([]InventoryItem, bool) {
	w.mu.RLock()
	defer w.mu.RUnlock()
	p, ok := w.players[id]
	if !ok {
		return nil, false
	}
	return append([]InventoryItem(nil), p.inventory...), true
}

func (w *World) HasHouseKey(id int64) bool {
	w.mu.RLock()
	defer w.mu.RUnlock()
	p, ok := w.players[id]
	if !ok {
		return false
	}
	for _, it := range p.inventory {
		if it.ItemID == db.ItemHouseKey && it.HouseID > 0 {
			return true
		}
	}
	return false
}

func playerHasHouseKey(items []InventoryItem) bool {
	for _, it := range items {
		if it.ItemID == db.ItemHouseKey && it.HouseID > 0 {
			return true
		}
	}
	return false
}
