package game

import "testing"

func TestRecipeForEgg(t *testing.T) {
	r, ok := recipeForIngredient("chicken_egg")
	if !ok || r.result != "fried_egg" {
		t.Fatalf("egg recipe = %+v ok=%v", r, ok)
	}
}

func TestRecipeForAnyFish(t *testing.T) {
	r, ok := recipeForIngredient("carp")
	if !ok || r.result != "baked_fish" {
		t.Fatalf("carp recipe = %+v ok=%v", r, ok)
	}
	if _, ok := recipeForIngredient("hoe"); ok {
		t.Fatal("hoe should not cook")
	}
}

func TestStarterKitchenOffset(t *testing.T) {
	item := starterKitchen(100, 200)
	if item.Type != "kitchen" {
		t.Fatalf("type %s", item.Type)
	}
	if item.X <= 100 || item.Y <= 200 {
		t.Fatalf("kitchen should sit in the east kitchen room, got %+v", item)
	}
}

func TestEnsureStarterKitchenOnce(t *testing.T) {
	p := &housePlot{centerX: 0, centerY: 0}
	if !p.ensureStarterKitchen() {
		t.Fatal("expected first kitchen")
	}
	if p.ensureStarterKitchen() {
		t.Fatal("should not add a second kitchen")
	}
	if len(p.furniture) != 1 || p.furniture[0].Type != "kitchen" {
		t.Fatalf("furniture %+v", p.furniture)
	}
}

func TestApplyCookActionCooksEgg(t *testing.T) {
	w := &World{
		players: make(map[int64]*player),
		housing: NewHousingIndex(),
	}
	kit := starterKitchen(0, 0)
	w.housing.byID[7] = &housePlot{
		id: 7, characterID: 1, centerX: 0, centerY: 0,
		furniture: []FurnitureItem{kit},
	}
	w.AddPlayer(1, "Cook", "human", PlayerAppearance{}, kit.X, kit.Y, nil, 0,
		[]InventoryItem{{ItemID: "chicken_egg", Count: 1, Slot: 0}}, "", PlayerVitals{})
	w.mu.Lock()
	w.players[1].insideHouseID = 7
	w.mu.Unlock()

	result, msg, ok := w.ApplyCookAction(1, "chicken_egg", 0)
	if !ok {
		t.Fatalf("cook failed: %s", msg)
	}
	if result.ResultID != "fried_egg" {
		t.Fatalf("result %s", result.ResultID)
	}
	hasMeal, hasEgg := false, false
	for _, it := range result.Inventory {
		if it.ItemID == "fried_egg" {
			hasMeal = true
		}
		if it.ItemID == "chicken_egg" {
			hasEgg = true
		}
	}
	if !hasMeal || hasEgg {
		t.Fatalf("inventory after cook: %+v", result.Inventory)
	}
}
