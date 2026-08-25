package game

import (
	"math"

	"github.com/deathborn/server/internal/skills"
)

// CookActionResult is returned after cooking a meal at a homestead station.
type CookActionResult struct {
	Inventory    []InventoryItem
	ResultID     string
	ResultName   string
	GrantedXP    int64
	Skill        string
	Xp           int64
	Level        int
	LeveledUp    bool
	TotalXp      int64
	PlayerAction string
}

func (w *World) ApplyCookAction(playerID int64, itemID string, slot int) (CookActionResult, string, bool) {
	w.mu.Lock()
	defer w.mu.Unlock()

	p, ok := w.players[playerID]
	if !ok || p.dead {
		return CookActionResult{}, "You cannot cook right now.", false
	}
	if p.insideHouseID <= 0 || w.housing == nil {
		return CookActionResult{}, "Cook inside a homestead kitchen.", false
	}
	plot := w.housing.byID[p.insideHouseID]
	if plot == nil {
		return CookActionResult{}, "Cook inside a homestead kitchen.", false
	}
	if !nearCookingStation(plot, p.x, p.y) {
		return CookActionResult{}, "Stand next to the kitchen or fireplace.", false
	}

	recipe, ok := recipeForIngredient(itemID)
	if !ok {
		return CookActionResult{}, "You cannot cook that.", false
	}

	slots := itemsToSlots(p.inventory)
	var took bool
	slots, took = takeItemsFromSlots(slots, itemID, 1, slot)
	if !took {
		return CookActionResult{}, "You do not have that ingredient.", false
	}
	meal := InventoryItem{ItemID: recipe.result, Count: 1}
	if !inventoryHasRoom(slots, meal) {
		return CookActionResult{}, "Your bags are full.", false
	}
	slots = addItemToSlots(slots, meal)
	p.inventory = slotsToItems(slots)

	result := CookActionResult{
		Inventory:    append([]InventoryItem(nil), p.inventory...),
		ResultID:     recipe.result,
		ResultName:   recipe.name,
		GrantedXP:    recipe.xp,
		Skill:        skills.Cooking,
		PlayerAction: "cook",
	}
	now := farmNow()
	if now-p.lastSkillInteract >= 1.2 {
		p.lastSkillInteract = now
		if p.skills == nil {
			p.skills = skills.NewSet()
		}
		newXp, level, leveled := p.skills.Grant(skills.Cooking, recipe.xp)
		p.totalXp = p.skills.TotalXP()
		result.Xp = newXp
		result.Level = level
		result.LeveledUp = leveled
		result.TotalXp = p.totalXp
	} else {
		result.GrantedXP = 0
	}
	return result, "", true
}

func nearCookingStation(plot *housePlot, x, y float64) bool {
	for _, f := range plot.furniture {
		if !IsCookingStation(f.Type) {
			continue
		}
		if math.Hypot(x-f.X, y-f.Y) <= CookActionRange {
			return true
		}
	}
	return false
}

// TakeDirtyFurniture returns homestead interiors that gained a starter kitchen.
func (w *World) TakeDirtyFurniture() []FurnitureSave {
	w.mu.Lock()
	defer w.mu.Unlock()
	if w.housing == nil {
		return nil
	}
	var out []FurnitureSave
	for _, p := range w.housing.byID {
		if !p.furnitureDirty {
			continue
		}
		p.furnitureDirty = false
		out = append(out, FurnitureSave{
			CharacterID: p.characterID,
			Items:       append([]FurnitureItem(nil), p.furniture...),
		})
	}
	return out
}

// FurnitureSave is furniture that needs to be written to the database.
type FurnitureSave struct {
	CharacterID int64
	Items       []FurnitureItem
}
