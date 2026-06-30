package net

import (
	"context"

	"github.com/deathborn/server/internal/db"
	"github.com/deathborn/server/internal/game"
)

// dropInventoryOnDeath spawns ground loot from a player's inventory at their corpse.
func (h *Hub) dropInventoryOnDeath(ctx context.Context, database *db.DB, playerID int64, x, y float64) {
	inv, ok := h.world.PlayerInventory(playerID)
	if !ok {
		dbInv, err := database.GetCharacterInventory(ctx, playerID)
		if err != nil {
			return
		}
		inv = game.InventoryFromDB(dbInv)
	}
	if len(inv) == 0 {
		return
	}

	for i, item := range inv {
		offsetX := float64((i%3)-1) * 14
		offsetY := float64(i/3)*12 + 10
		row, err := database.CreateWorldItemDrop(ctx, db.WorldItemDrop{
			ItemID: item.ItemID, HouseID: item.HouseID, Count: item.Count,
			X: x + offsetX, Y: y + offsetY, SourceCharacterID: playerID,
		})
		if err == nil {
			h.world.RegisterDrop(row)
		}
	}

	_ = database.SaveCharacterInventory(ctx, playerID, nil)
	h.world.ClearInventory(playerID)
}
