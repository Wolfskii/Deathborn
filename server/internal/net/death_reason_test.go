package net

import (
	"testing"

	"github.com/deathborn/server/internal/game"
)

func TestDeathReason(t *testing.T) {
	tests := []struct {
		name       string
		killerID   int64
		killerName string
		wantReason string
		wantKiller string
	}{
		{"player killer", 7, "Rowan", "Slain by Rowan", "Rowan"},
		{"boss killer", -1000, "Iron Colossus", "Killed by Iron Colossus", "Iron Colossus"},
		{"mob killer", -2004, "Forest Slime", "Killed by Forest Slime", "Forest Slime"},
		{"no killer", 0, "", "Died in the wild", ""},
		{"unknown name falls back", 7, "", "Died in the wild", ""},
	}
	for _, tc := range tests {
		t.Run(tc.name, func(t *testing.T) {
			reason, killer := deathReason(tc.killerID, tc.killerName)
			if reason != tc.wantReason || killer != tc.wantKiller {
				t.Fatalf("deathReason = (%q,%q), want (%q,%q)",
					reason, killer, tc.wantReason, tc.wantKiller)
			}
		})
	}
}

func TestDeathSummary(t *testing.T) {
	got := deathSummary(14, 12*60+30, 2)
	want := "Total level 14, 12 min survived, 2 kills"
	if got != want {
		t.Fatalf("deathSummary = %q, want %q", got, want)
	}
	if got := deathSummary(3, 3*3600+5*60, 1); got != "Total level 3, 3h 5m survived, 1 kill" {
		t.Fatalf("long survival / singular kill: got %q", got)
	}
}

func TestInventoryWealthSumsCounts(t *testing.T) {
	items := []game.InventoryItem{{ItemID: "a", Count: 3}, {ItemID: "b", Count: 5}}
	if got := inventoryWealth(items); got != 8 {
		t.Fatalf("inventoryWealth = %d, want 8", got)
	}
	if got := inventoryWealth(nil); got != 0 {
		t.Fatalf("empty inventory wealth = %d, want 0", got)
	}
}
