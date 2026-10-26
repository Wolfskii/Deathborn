package game

import (
	"math/rand"
	"testing"
)

func TestRollDropsAlwaysAndNever(t *testing.T) {
	rng := rand.New(rand.NewSource(1))
	always := rollDrops([]dropEntry{{itemID: "bone", chance: 1, min: 2, max: 2}}, rng)
	if len(always) != 1 || always[0].ItemID != "bone" || always[0].Count != 2 {
		t.Fatalf("chance 1 should always drop 2 bone, got %+v", always)
	}
	for i := 0; i < 50; i++ {
		if got := rollDrops([]dropEntry{{itemID: "bone", chance: 0, min: 1, max: 1}}, rng); len(got) != 0 {
			t.Fatalf("chance 0 should never drop, got %+v", got)
		}
	}
}

func TestRollDropsCountWithinRange(t *testing.T) {
	rng := rand.New(rand.NewSource(7))
	entry := []dropEntry{{itemID: "slime_gel", chance: 1, min: 1, max: 3}}
	seen := map[int]bool{}
	for i := 0; i < 200; i++ {
		got := rollDrops(entry, rng)
		if len(got) != 1 || got[0].Count < 1 || got[0].Count > 3 {
			t.Fatalf("count out of range: %+v", got)
		}
		seen[got[0].Count] = true
	}
	if len(seen) != 3 {
		t.Fatalf("expected all counts 1..3 to appear, saw %v", seen)
	}
}

func TestRollNpcLootUnknownDefIsEmpty(t *testing.T) {
	if got := RollNpcLoot("no_such_npc", rand.New(rand.NewSource(1))); got != nil {
		t.Fatalf("unknown def should drop nothing, got %+v", got)
	}
}

func TestRollNpcLootBossAlwaysDropsTrophy(t *testing.T) {
	rng := rand.New(rand.NewSource(3))
	got := RollNpcLoot("iron_colossus", rng)
	var trophy bool
	for _, it := range got {
		if it.ItemID == "boss_trophy" {
			trophy = true
		}
	}
	if !trophy {
		t.Fatalf("boss should always drop its trophy, got %+v", got)
	}
}

func TestNpcKillXP(t *testing.T) {
	def, ok := lookupNpcDef("forest_orc")
	if !ok {
		t.Fatal("forest_orc missing")
	}
	if got, want := NpcKillXP("forest_orc"), int64(def.hpMax/2); got != want {
		t.Fatalf("mob kill xp = %d, want %d", got, want)
	}
	if NpcKillXP("no_such_npc") != 0 {
		t.Fatal("unknown npc should give no kill xp")
	}
}
