package game

import (
	"strings"
	"testing"

	"github.com/deathborn/server/internal/skills"
)

// newSmeltWorld places a player inside a homestead with a starter kitchen,
// holding the given ore.
func newSmeltWorld(t *testing.T, ore string) *World {
	t.Helper()
	w := &World{players: make(map[int64]*player), housing: NewHousingIndex()}
	kit := starterKitchen(0, 0)
	w.housing.byID[7] = &housePlot{id: 7, characterID: 1, centerX: 0, centerY: 0, furniture: []FurnitureItem{kit}}
	w.AddPlayer(1, "Smith", "human", PlayerAppearance{}, kit.X, kit.Y, nil, 0,
		[]InventoryItem{{ItemID: ore, Count: 1, Slot: 0}}, "", PlayerVitals{})
	w.mu.Lock()
	w.players[1].insideHouseID = 7
	w.players[1].skills = skills.NewSet()
	w.mu.Unlock()
	return w
}

func TestSmeltCopperOreTrainsSmithing(t *testing.T) {
	w := newSmeltWorld(t, "copper_ore")
	res, msg, ok := w.ApplyCookAction(1, "copper_ore", 0)
	if !ok {
		t.Fatalf("smelting failed: %s", msg)
	}
	if res.ResultID != "copper_bar" || res.Skill != skills.Smithing {
		t.Fatalf("result = %+v, want copper_bar trained in smithing", res)
	}
	if got := w.players[1].skills.Level(skills.Smithing); got < 1 || w.players[1].skills[skills.Smithing] == 0 {
		t.Fatalf("smithing xp not granted: %+v", w.players[1].skills)
	}
}

func TestSmeltIronNeedsSmithingLevel(t *testing.T) {
	w := newSmeltWorld(t, "iron_ore")
	_, msg, ok := w.ApplyCookAction(1, "iron_ore", 0)
	if ok || !strings.Contains(msg, "too low") {
		t.Fatalf("iron at level 1: ok=%v msg=%q, want level too low", ok, msg)
	}
	if n := len(w.players[1].inventory); n != 1 || w.players[1].inventory[0].ItemID != "iron_ore" {
		t.Fatalf("a refused smelt must keep the ore, inventory = %+v", w.players[1].inventory)
	}

	w.players[1].skills[skills.Smithing] = skills.XpForLevel(15)
	res, msg, ok := w.ApplyCookAction(1, "iron_ore", 0)
	if !ok || res.ResultID != "iron_bar" {
		t.Fatalf("iron at level 15: ok=%v msg=%q res=%+v", ok, msg, res)
	}
}

func TestBarsAreNotMeals(t *testing.T) {
	if IsMealItem("copper_bar") || IsMealItem("iron_bar") {
		t.Fatal("bars must not count as meals")
	}
	if !IsMealItem("bread") {
		t.Fatal("bread should still be a meal")
	}
}
