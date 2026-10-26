package net

import (
	"context"
	"math/rand"
	"time"

	"github.com/deathborn/server/internal/db"
	"github.com/deathborn/server/internal/game"
)

// dropNpcLoot rolls a killed boss's or mob's loot table and places the items
// on the ground as lootable drops, the same way a death drops inventory.
func (h *Hub) dropNpcLoot(ctx context.Context, database *db.DB, defID string, x, y float64) {
	rng := rand.New(rand.NewSource(time.Now().UnixNano()))
	items := game.RollNpcLoot(defID, rng)
	for i, item := range items {
		offsetX := float64((i%3)-1) * 14
		offsetY := float64(i/3)*12 + 10
		row, err := database.CreateWorldItemDrop(ctx, db.WorldItemDrop{
			ItemID: item.ItemID, Count: item.Count,
			X: x + offsetX, Y: y + offsetY,
		})
		if err == nil {
			h.world.RegisterDrop(row)
		}
	}
}
