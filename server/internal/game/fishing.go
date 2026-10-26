package game

import (
	"math"
	"math/rand/v2"
)

const (
	FishTileSize     = 16.0
	FishActionRange  = 48.0
	FishMinWaitSec   = 1.1
	FishMaxChebyshev = 3
)

type fishDef struct {
	id     string
	name   string
	sea    bool
	level  int
	xp     int64
	weight int
	rare   bool
}

var fishDefs = []fishDef{
	{id: "sunfish", name: "Sunfish", level: 1, xp: 18, weight: 32},
	{id: "chub", name: "Chub", level: 1, xp: 20, weight: 28},
	{id: "perch", name: "Perch", level: 3, xp: 26, weight: 22},
	{id: "carp", name: "Carp", level: 4, xp: 30, weight: 18},
	{id: "largemouth_bass", name: "Largemouth Bass", level: 6, xp: 38, weight: 12},
	{id: "pike", name: "Pike", level: 8, xp: 46, weight: 8},
	{id: "tiger_trout", name: "Tiger Trout", level: 9, xp: 50, weight: 7},
	{id: "walleye", name: "Walleye", level: 10, xp: 54, weight: 6},
	{id: "sturgeon", name: "Sturgeon", level: 14, xp: 70, weight: 3},
	{id: "golden_fish", name: "Golden Fish", level: 18, xp: 95, weight: 1, rare: true},
	{id: "anchovy", name: "Anchovy", sea: true, level: 1, xp: 18, weight: 30},
	{id: "sardine", name: "Sardine", sea: true, level: 1, xp: 20, weight: 26},
	{id: "herring", name: "Herring", sea: true, level: 3, xp: 24, weight: 20},
	{id: "salmon", name: "Salmon", sea: true, level: 5, xp: 36, weight: 14},
	{id: "red_snapper", name: "Red Snapper", sea: true, level: 6, xp: 40, weight: 12},
	{id: "tuna", name: "Tuna", sea: true, level: 8, xp: 48, weight: 9},
	{id: "flounder", name: "Flounder", sea: true, level: 7, xp: 42, weight: 10},
	{id: "pufferfish", name: "Pufferfish", sea: true, level: 11, xp: 58, weight: 5},
	{id: "albacore", name: "Albacore", sea: true, level: 12, xp: 62, weight: 4},
	{id: "anglerfish", name: "Anglerfish", sea: true, level: 20, xp: 100, weight: 1, rare: true},
}

// fishWeightKg is the min and max weight a catch of each fish can have.
var fishWeightKg = map[string][2]float64{
	"sunfish": {0.1, 0.5}, "chub": {0.2, 1.2}, "perch": {0.2, 1.5}, "carp": {1.0, 8.0},
	"largemouth_bass": {0.8, 4.5}, "pike": {1.5, 12.0}, "tiger_trout": {0.8, 5.0},
	"walleye": {0.7, 5.5}, "sturgeon": {6.0, 40.0}, "golden_fish": {0.4, 2.0},
	"anchovy": {0.02, 0.08}, "sardine": {0.05, 0.2}, "herring": {0.1, 0.6}, "salmon": {2.0, 12.0},
	"red_snapper": {1.0, 9.0}, "tuna": {8.0, 60.0}, "flounder": {0.5, 3.0}, "pufferfish": {0.3, 2.0},
	"albacore": {6.0, 30.0}, "anglerfish": {2.0, 25.0},
}

// rollFishWeight picks a catch weight in kg (two decimals). Small fish are common, trophies
// rare, and higher fishing levels nudge the roll up a little.
func rollFishWeight(id string, level int, rng func() float64) float64 {
	r, ok := fishWeightKg[id]
	if !ok {
		r = [2]float64{0.2, 2.0}
	}
	t := rng() * rng() // skewed toward the light end
	t += float64(min(level, 30)) * 0.006 * rng()
	if t > 1 {
		t = 1
	}
	kg := r[0] + (r[1]-r[0])*t
	return math.Round(kg*100) / 100
}

var fishByID = func() map[string]fishDef {
	m := make(map[string]fishDef, len(fishDefs))
	for _, d := range fishDefs {
		m[d.id] = d
	}
	return m
}()

func IsFishItem(itemID string) bool {
	_, ok := fishByID[itemID]
	return ok
}

func IsFishingTool(itemID string) bool {
	return itemID == "fishing_rod"
}

func FishingItemMaxStack(itemID string) int {
	if itemID == "fishing_rod" {
		return 1
	}
	if itemID == "worm_bait" {
		return 40
	}
	if IsFishItem(itemID) {
		return 40
	}
	return 0
}

func FishingStarterKit() []InventoryItem {
	return []InventoryItem{
		{ItemID: "fishing_rod", Count: 1},
		{ItemID: "worm_bait", Count: 25},
	}
}

func FishName(itemID string) string {
	if d, ok := fishByID[itemID]; ok {
		return d.name
	}
	return itemID
}

func rollFish(level int, sea, baited bool) fishDef {
	if level < 1 {
		level = 1
	}
	total := 0
	var picks []fishDef
	for _, d := range fishDefs {
		if d.sea != sea {
			continue
		}
		if d.level > level+2 {
			continue
		}
		w := d.weight
		if d.level > level {
			w = max(1, w/3)
		}
		if baited && d.rare {
			w *= 4
		} else if baited && d.level >= 6 {
			w *= 2
		}
		if w <= 0 {
			continue
		}
		picks = append(picks, fishDef{id: d.id, name: d.name, sea: d.sea, level: d.level, xp: d.xp, weight: w, rare: d.rare})
		total += w
	}
	if total <= 0 {
		if sea {
			return fishByID["anchovy"]
		}
		return fishByID["sunfish"]
	}
	n := rand.IntN(total)
	for _, d := range picks {
		n -= d.weight
		if n < 0 {
			return fishByID[d.id]
		}
	}
	return picks[len(picks)-1]
}
