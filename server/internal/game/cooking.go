package game

import "github.com/deathborn/server/internal/skills"

const (
	CookActionRange = 48.0
)

type cookRecipe struct {
	id         string
	name       string
	result     string
	heal       int
	stamina    int
	xp         int64
	anyFish    bool
	ingredient string
	skill      string // trained skill; empty means cooking
	minLevel   int    // level needed in skill
}

// trainedSkill returns the skill a recipe trains (cooking unless set).
func (r cookRecipe) trainedSkill() string {
	if r.skill == "" {
		return skills.Cooking
	}
	return r.skill
}

var cookRecipes = []cookRecipe{
	{id: "fried_egg", name: "Fried Egg", result: "fried_egg", ingredient: "chicken_egg", heal: 22, stamina: 8, xp: 18},
	{id: "parsnip_soup", name: "Parsnip Soup", result: "parsnip_soup", ingredient: "parsnip", heal: 26, stamina: 6, xp: 20},
	{id: "baked_potato", name: "Baked Potato", result: "baked_potato", ingredient: "potato", heal: 24, stamina: 6, xp: 20},
	{id: "pumpkin_pie", name: "Pumpkin Pie", result: "pumpkin_pie", ingredient: "pumpkin", heal: 42, stamina: 12, xp: 36},
	{id: "bread", name: "Bread", result: "bread", ingredient: "wheat", heal: 18, stamina: 10, xp: 16},
	{id: "jam", name: "Jam", result: "jam", ingredient: "strawberry", heal: 16, stamina: 18, xp: 22},
	{id: "baked_fish", name: "Baked Fish", result: "baked_fish", anyFish: true, heal: 32, stamina: 8, xp: 24},
	// Smelting: ore at a kitchen or fireplace becomes a bar, trained in smithing.
	{id: "copper_bar", name: "Copper Bar", result: "copper_bar", ingredient: "copper_ore", xp: 40, skill: skills.Smithing, minLevel: 1},
	{id: "iron_bar", name: "Iron Bar", result: "iron_bar", ingredient: "iron_ore", xp: 80, skill: skills.Smithing, minLevel: 15},
}

var cookByIngredient = func() map[string]cookRecipe {
	m := make(map[string]cookRecipe, len(cookRecipes))
	for _, r := range cookRecipes {
		if r.ingredient != "" {
			m[r.ingredient] = r
		}
	}
	return m
}()

func recipeForIngredient(itemID string) (cookRecipe, bool) {
	if r, ok := cookByIngredient[itemID]; ok {
		return r, true
	}
	if IsFishItem(itemID) {
		for _, r := range cookRecipes {
			if r.anyFish {
				return r, true
			}
		}
	}
	return cookRecipe{}, false
}

func IsMealItem(itemID string) bool {
	for _, r := range cookRecipes {
		if r.result == itemID && r.trainedSkill() == skills.Cooking {
			return true
		}
	}
	return false
}

func IsCookingStation(furnitureType string) bool {
	return furnitureType == "kitchen" || furnitureType == "fireplace"
}

func CookingItemMaxStack(itemID string) int {
	if IsMealItem(itemID) {
		return 20
	}
	return 0
}

func MealName(itemID string) string {
	for _, r := range cookRecipes {
		if r.result == itemID {
			return r.name
		}
	}
	return itemID
}

// starterKitchen stands against the north wall of the kitchen (east room).
func starterKitchen(centerX, centerY float64) FurnitureItem {
	return FurnitureItem{
		Type: "kitchen",
		X:    interiorTileX(centerX, 21) + interiorTile/2,
		Y:    interiorTileY(centerY, 4) + interiorTile/2,
	}
}

func (p *housePlot) ensureStarterKitchen() bool {
	for _, f := range p.furniture {
		if IsCookingStation(f.Type) {
			return false
		}
	}
	p.furniture = append(p.furniture, starterKitchen(p.centerX, p.centerY))
	return true
}
