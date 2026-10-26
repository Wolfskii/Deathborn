package game

import "math/rand"

// dropEntry is one line of a loot table: an item that drops with the given
// chance, in a stack size between min and max inclusive.
type dropEntry struct {
	itemID   string
	chance   float64 // 0..1
	min, max int
}

// bossLoot is shared by every world boss.
var bossLoot = []dropEntry{
	{itemID: "boss_trophy", chance: 1, min: 1, max: 1},
	{itemID: "health_potion", chance: 1, min: 3, max: 3},
}

// rollDrops rolls each entry once and returns the items that dropped.
// Slots are left at zero; the caller places them.
func rollDrops(entries []dropEntry, rng *rand.Rand) []InventoryItem {
	var out []InventoryItem
	for _, e := range entries {
		if rng.Float64() >= e.chance {
			continue
		}
		count := e.min
		if e.max > e.min {
			count += rng.Intn(e.max - e.min + 1)
		}
		if count <= 0 {
			continue
		}
		out = append(out, InventoryItem{ItemID: e.itemID, Count: count})
	}
	return out
}

// RollNpcLoot rolls the loot table for a killed mob or boss definition.
func RollNpcLoot(defID string, rng *rand.Rand) []InventoryItem {
	if def, ok := lookupNpcDef(defID); ok {
		return rollDrops(def.drops, rng)
	}
	for _, b := range bossDefs {
		if b.id == defID {
			return rollDrops(bossLoot, rng)
		}
	}
	return nil
}

// NpcKillXP is the Strength bonus for finishing an NPC: half its max HP for
// mobs, a quarter of its max HP for bosses (bosses are tougher, so the
// per-hit XP already covers more of their value).
func NpcKillXP(defID string) int64 {
	if def, ok := lookupNpcDef(defID); ok {
		return int64(def.hpMax / 2)
	}
	for _, b := range bossDefs {
		if b.id == defID {
			return int64(b.hpMax / 4)
		}
	}
	return 0
}
