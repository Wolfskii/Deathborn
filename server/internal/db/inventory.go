package db

import (
	"context"
	"encoding/json"
	"errors"

	"github.com/jackc/pgx/v5"
)

const ItemHouseKey = "house_key"

// InventoryItem is one stack in a character's inventory.
type InventoryItem struct {
	Slot    int    `json:"slot,omitempty"`
	ItemID  string `json:"itemId"`
	Count   int    `json:"count"`
	HouseID int64  `json:"houseId,omitempty"`
}

// GetCharacterInventory returns a character's inventory JSON.
func (d *DB) GetCharacterInventory(ctx context.Context, characterID int64) ([]InventoryItem, error) {
	var raw []byte
	err := d.Pool.QueryRow(ctx,
		`SELECT inventory FROM characters WHERE id = $1`,
		characterID,
	).Scan(&raw)
	if errors.Is(err, pgx.ErrNoRows) {
		return nil, ErrNotFound
	}
	if err != nil {
		return nil, err
	}
	return decodeInventory(raw), nil
}

// SaveCharacterInventory replaces a character's inventory.
func (d *DB) SaveCharacterInventory(ctx context.Context, characterID int64, items []InventoryItem) error {
	items = normalizeInventorySlots(items)
	raw, err := json.Marshal(items)
	if err != nil {
		return err
	}
	tag, err := d.Pool.Exec(ctx,
		`UPDATE characters SET inventory = $2 WHERE id = $1`,
		characterID, raw,
	)
	if err != nil {
		return err
	}
	if tag.RowsAffected() == 0 {
		return ErrNotFound
	}
	return nil
}

// GrantHouseKey appends a homestead key for the given house.
func (d *DB) GrantHouseKey(ctx context.Context, characterID, houseID int64) ([]InventoryItem, error) {
	items, err := d.GetCharacterInventory(ctx, characterID)
	if err != nil {
		return nil, err
	}
	items = normalizeInventorySlots(items)
	for _, it := range items {
		if it.ItemID == ItemHouseKey && it.HouseID == houseID {
			return items, nil
		}
	}
	slot := firstEmptySlot(items)
	if slot < 0 {
		return items, errors.New("inventory full")
	}
	items = append(items, InventoryItem{Slot: slot, ItemID: ItemHouseKey, Count: 1, HouseID: houseID})
	items = normalizeInventorySlots(items)
	if err := d.SaveCharacterInventory(ctx, characterID, items); err != nil {
		return nil, err
	}
	return items, nil
}

// StarterInventory matches the client default loadout.
func StarterInventory() []InventoryItem {
	return normalizeInventorySlots([]InventoryItem{
		{Slot: 0, ItemID: "bandage", Count: 5},
		{Slot: 1, ItemID: "health_potion", Count: 3},
		{Slot: 2, ItemID: "mana_potion", Count: 2},
		{Slot: 3, ItemID: "stamina_potion", Count: 2},
		{Slot: 4, ItemID: "antidote", Count: 1},
	})
}

// InitCharacterInventory sets starter items for a new character.
func (d *DB) InitCharacterInventory(ctx context.Context, characterID int64) error {
	return d.SaveCharacterInventory(ctx, characterID, StarterInventory())
}

// DeleteHousesForDeadCharactersWithKeys removes plots whose dead owners still hold the key
// (unrecoverable — e.g. character removed before the key was dropped).
func (d *DB) DeleteHousesForDeadCharactersWithKeys(ctx context.Context, accountID int64) ([]int64, error) {
	rows, err := d.Pool.Query(ctx,
		`SELECT c.id, c.inventory
		 FROM characters c
		 WHERE c.account_id = $1 AND c.alive = FALSE`,
		accountID,
	)
	if err != nil {
		return nil, err
	}
	defer rows.Close()

	var removed []int64
	for rows.Next() {
		var charID int64
		var raw []byte
		if err := rows.Scan(&charID, &raw); err != nil {
			return removed, err
		}
		for _, it := range decodeInventory(raw) {
			if it.ItemID != ItemHouseKey || it.HouseID <= 0 {
				continue
			}
			tag, err := d.Pool.Exec(ctx, `DELETE FROM houses WHERE id = $1`, it.HouseID)
			if err != nil {
				return removed, err
			}
			if tag.RowsAffected() > 0 {
				removed = append(removed, it.HouseID)
			}
		}
	}
	return removed, rows.Err()
}

func decodeInventory(raw []byte) []InventoryItem {
	if len(raw) == 0 {
		return nil
	}
	var items []InventoryItem
	if json.Unmarshal(raw, &items) != nil {
		return nil
	}
	return normalizeInventorySlots(items)
}

const inventorySlotCount = 20

func normalizeInventorySlots(items []InventoryItem) []InventoryItem {
	var slots [inventorySlotCount]InventoryItem
	next := 0
	for _, it := range items {
		if it.Count <= 0 || it.ItemID == "" {
			continue
		}
		slot := it.Slot
		if slot < 0 || slot >= inventorySlotCount || slots[slot].ItemID != "" {
			for slot = next; slot < inventorySlotCount; slot++ {
				if slots[slot].ItemID == "" {
					break
				}
			}
			next = slot + 1
		}
		if slot < 0 || slot >= inventorySlotCount {
			continue
		}
		slots[slot] = InventoryItem{
			Slot: slot, ItemID: it.ItemID, Count: it.Count, HouseID: it.HouseID,
		}
	}
	out := make([]InventoryItem, 0, inventorySlotCount)
	for i := 0; i < inventorySlotCount; i++ {
		if slots[i].ItemID == "" || slots[i].Count <= 0 {
			continue
		}
		slots[i].Slot = i
		out = append(out, slots[i])
	}
	return out
}

func firstEmptySlot(items []InventoryItem) int {
	var slots [inventorySlotCount]bool
	for _, it := range items {
		if it.Slot >= 0 && it.Slot < inventorySlotCount {
			slots[it.Slot] = true
		}
	}
	for i := 0; i < inventorySlotCount; i++ {
		if !slots[i] {
			return i
		}
	}
	return -1
}
