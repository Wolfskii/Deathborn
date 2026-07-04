package game

import (
	"math"

	"github.com/deathborn/server/internal/db"
)

const (
	PickupRange      = 48.0
	InventorySlotCount = 20
)

// InventoryItem is one stack in a fixed inventory slot.
type InventoryItem struct {
	Slot    int    `json:"slot,omitempty"`
	ItemID  string `json:"itemId"`
	Count   int    `json:"count"`
	HouseID int64  `json:"houseId,omitempty"`
}

func inventoryFromDB(items []db.InventoryItem) []InventoryItem {
	out := make([]InventoryItem, 0, len(items))
	for _, it := range items {
		out = append(out, InventoryItem{
			Slot: it.Slot, ItemID: it.ItemID, Count: it.Count, HouseID: it.HouseID,
		})
	}
	return NormalizeInventory(out)
}

func inventoryToDB(items []InventoryItem) []db.InventoryItem {
	items = NormalizeInventory(items)
	out := make([]db.InventoryItem, 0, len(items))
	for _, it := range items {
		out = append(out, db.InventoryItem{
			Slot: it.Slot, ItemID: it.ItemID, Count: it.Count, HouseID: it.HouseID,
		})
	}
	return out
}

func InventoryFromDB(items []db.InventoryItem) []InventoryItem {
	return inventoryFromDB(items)
}

func InventoryToDB(items []InventoryItem) []db.InventoryItem {
	return inventoryToDB(items)
}

func maxStack(itemID string) int {
	if IsCosmeticItem(itemID) || itemID == "house_key" {
		return 1
	}
	switch itemID {
	case "bandage", "antidote":
		return 10
	default:
		return 20
	}
}

func itemsMatch(a, b InventoryItem) bool {
	return a.ItemID == b.ItemID && a.HouseID == b.HouseID
}

func itemsToSlots(items []InventoryItem) [InventorySlotCount]InventoryItem {
	var slots [InventorySlotCount]InventoryItem
	next := 0
	for _, it := range items {
		if it.Count <= 0 || it.ItemID == "" {
			continue
		}
		slot := it.Slot
		if slot < 0 || slot >= InventorySlotCount || slots[slot].ItemID != "" {
			for slot = next; slot < InventorySlotCount; slot++ {
				if slots[slot].ItemID == "" {
					break
				}
			}
			next = slot + 1
		}
		if slot < 0 || slot >= InventorySlotCount {
			continue
		}
		slots[slot] = InventoryItem{
			Slot: slot, ItemID: it.ItemID, Count: it.Count, HouseID: it.HouseID,
		}
	}
	return slots
}

func slotsToItems(slots [InventorySlotCount]InventoryItem) []InventoryItem {
	out := make([]InventoryItem, 0, InventorySlotCount)
	for i := 0; i < InventorySlotCount; i++ {
		if slots[i].ItemID == "" || slots[i].Count <= 0 {
			continue
		}
		slots[i].Slot = i
		out = append(out, slots[i])
	}
	return out
}

// NormalizeInventory assigns stable slot indices to every stack.
func NormalizeInventory(items []InventoryItem) []InventoryItem {
	return slotsToItems(itemsToSlots(items))
}

func addItemToSlots(slots [InventorySlotCount]InventoryItem, item InventoryItem) [InventorySlotCount]InventoryItem {
	if item.Count <= 0 || item.ItemID == "" {
		return slots
	}
	remaining := item.Count
	for i := 0; i < InventorySlotCount && remaining > 0; i++ {
		if !itemsMatch(slots[i], item) || slots[i].ItemID == "" {
			continue
		}
		cap := maxStack(item.ItemID) - slots[i].Count
		if cap <= 0 {
			continue
		}
		add := remaining
		if add > cap {
			add = cap
		}
		slots[i].Count += add
		remaining -= add
	}
	for i := 0; i < InventorySlotCount && remaining > 0; i++ {
		if slots[i].ItemID != "" {
			continue
		}
		add := remaining
		if add > maxStack(item.ItemID) {
			add = maxStack(item.ItemID)
		}
		slots[i] = InventoryItem{
			Slot: i, ItemID: item.ItemID, Count: add, HouseID: item.HouseID,
		}
		remaining -= add
	}
	return slots
}

func moveInventorySlots(slots [InventorySlotCount]InventoryItem, from, to int) ([InventorySlotCount]InventoryItem, string, bool) {
	if from < 0 || from >= InventorySlotCount || to < 0 || to >= InventorySlotCount {
		return slots, "Invalid slot.", false
	}
	if from == to {
		return slots, "", true
	}
	src := slots[from]
	if src.ItemID == "" || src.Count <= 0 {
		return slots, "That slot is empty.", false
	}

	dst := slots[to]
	if dst.ItemID == "" {
		slots[to] = src
		slots[from] = InventoryItem{}
		return slots, "", true
	}
	if itemsMatch(src, dst) {
		cap := maxStack(src.ItemID) - dst.Count
		if cap <= 0 {
			return slots, "Stack is full.", false
		}
		move := src.Count
		if move > cap {
			move = cap
		}
		dst.Count += move
		src.Count -= move
		slots[to] = dst
		if src.Count <= 0 {
			slots[from] = InventoryItem{}
		} else {
			slots[from] = src
		}
		return slots, "", true
	}

	slots[from], slots[to] = dst, src
	return slots, "", true
}

func (w *World) SetPlayerInventory(id int64, items []InventoryItem) {
	w.mu.Lock()
	defer w.mu.Unlock()
	if p, ok := w.players[id]; ok {
		p.inventory = NormalizeInventory(items)
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

func (w *World) MoveInventorySlot(characterID int64, from, to int) ([]InventoryItem, string, bool) {
	w.mu.Lock()
	defer w.mu.Unlock()
	p, ok := w.players[characterID]
	if !ok {
		return nil, "Not in world.", false
	}
	slots := itemsToSlots(p.inventory)
	slots, msg, ok := moveInventorySlots(slots, from, to)
	if !ok {
		return nil, msg, false
	}
	p.inventory = slotsToItems(slots)
	syncPlayerCosmeticInventory(p)
	return append([]InventoryItem(nil), p.inventory...), "", true
}

func (w *World) DropInventorySlot(characterID int64, slot int, x, y float64) ([]InventoryItem, WorldItemDropState, string, bool) {
	w.mu.Lock()
	defer w.mu.Unlock()
	p, ok := w.players[characterID]
	if !ok || p.dead {
		return nil, WorldItemDropState{}, "Cannot drop right now.", false
	}
	if slot < 0 || slot >= InventorySlotCount {
		return nil, WorldItemDropState{}, "Invalid slot.", false
	}
	slots := itemsToSlots(p.inventory)
	item := slots[slot]
	if item.ItemID == "" || item.Count <= 0 {
		return nil, WorldItemDropState{}, "That slot is empty.", false
	}
	dropX, dropY := clampDropPosition(p.x, p.y, x, y)
	dropped := item
	slots[slot] = InventoryItem{}
	p.inventory = slotsToItems(slots)
	syncPlayerCosmeticInventory(p)
	return append([]InventoryItem(nil), p.inventory...), WorldItemDropState{
		ItemID: dropped.ItemID, Count: dropped.Count, HouseID: dropped.HouseID, X: dropX, Y: dropY,
	}, "", true
}

func clampDropPosition(playerX, playerY, x, y float64) (float64, float64) {
	const dropRange = 96.0
	if math.Hypot(x-playerX, y-playerY) <= dropRange {
		return x, y
	}
	return playerX + 8, playerY + 10
}

func (w *World) AddInventoryItem(characterID int64, item InventoryItem) []InventoryItem {
	w.mu.Lock()
	defer w.mu.Unlock()
	p, ok := w.players[characterID]
	if !ok {
		return nil
	}
	slots := itemsToSlots(p.inventory)
	slots = addItemToSlots(slots, item)
	p.inventory = slotsToItems(slots)
	return append([]InventoryItem(nil), p.inventory...)
}

func (w *World) ClearInventory(characterID int64) {
	w.mu.Lock()
	defer w.mu.Unlock()
	if p, ok := w.players[characterID]; ok {
		p.inventory = nil
		p.headCosmetic = ""
	}
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

func FirstEmptyInventorySlot(items []InventoryItem) int {
	slots := itemsToSlots(items)
	for i := 0; i < InventorySlotCount; i++ {
		if slots[i].ItemID == "" {
			return i
		}
	}
	return -1
}
