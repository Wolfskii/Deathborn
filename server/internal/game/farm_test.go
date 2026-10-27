package game

import (
	"math"
	"testing"
)

func TestHarvestReadyCrop(t *testing.T) {
	plot := &housePlot{centerX: 100, centerY: 100, nextAnimalID: 1}
	plot.crops = []farmTile{{tx: 6, ty: 8, crop: "parsnip", stage: 4}}
	var slots [InventorySlotCount]InventoryItem
	msg, xp, ok := harvestTile(plot, 0, &slots, 0)
	if !ok {
		t.Fatalf("harvest failed: %s", msg)
	}
	if xp <= 0 {
		t.Fatalf("expected farming xp, got %d", xp)
	}
	items := slotsToItems(slots)
	gotProduce, gotSeed := false, false
	for _, it := range items {
		if it.ItemID == "parsnip" && it.Count >= 1 {
			gotProduce = true
		}
		if it.ItemID == "parsnip_seeds" && it.Count >= 1 {
			gotSeed = true
		}
	}
	if !gotProduce || !gotSeed {
		t.Fatalf("loot missing produce/seed: %+v", items)
	}
	if plot.crops[0].crop != "" {
		t.Fatalf("parsnip should be pulled, still %q stage %d", plot.crops[0].crop, plot.crops[0].stage)
	}
}

func TestRegrowCropStaysPlanted(t *testing.T) {
	plot := &housePlot{centerX: 100, centerY: 100}
	plot.crops = []farmTile{{tx: 1, ty: 1, crop: "strawberry", stage: 5}}
	var slots [InventorySlotCount]InventoryItem
	if _, _, ok := harvestTile(plot, 0, &slots, 0); !ok {
		t.Fatal("harvest failed")
	}
	if plot.crops[0].crop != "strawberry" || plot.crops[0].stage != 3 {
		t.Fatalf("expected regrow stage 3, got %q %d", plot.crops[0].crop, plot.crops[0].stage)
	}
}

func TestTickFarmGrowsWhenWatered(t *testing.T) {
	now := farmNow()
	plot := &housePlot{centerX: 100, centerY: 100}
	plot.crops = []farmTile{{
		tx: 2, ty: 3, crop: "parsnip", stage: 0,
		wateredUntil: now + 1000,
	}}
	if !plot.tickFarm(cropDefs["parsnip"].stageSec+0.01, now) {
		t.Fatal("expected growth")
	}
	if plot.crops[0].stage != 1 {
		t.Fatalf("stage = %d, want 1", plot.crops[0].stage)
	}
}

func TestTickFarmPausedWhenDry(t *testing.T) {
	now := farmNow()
	plot := &housePlot{centerX: 100, centerY: 100}
	plot.crops = []farmTile{{
		tx: 2, ty: 3, crop: "parsnip", stage: 0,
		wateredUntil: now - 1,
	}}
	plot.tickFarm(100, now)
	if plot.crops[0].stage != 0 {
		t.Fatalf("dry crop grew to stage %d", plot.crops[0].stage)
	}
}

func TestSeedLookup(t *testing.T) {
	d, ok := cropDefBySeed("tomato_seeds")
	if !ok || d.id != "tomato" {
		t.Fatalf("tomato_seeds -> %+v ok=%v", d, ok)
	}
	if !IsFarmTool("hoe") || !IsFarmAnimalItem("chicken") {
		t.Fatal("tool/animal item checks failed")
	}
}

// Harvesting must not send a tool swing: the swing locks movement until it finishes.
func TestHarvestSendsNoToolSwing(t *testing.T) {
	const cx, cy = 500.0, 300.0
	w := &World{players: make(map[int64]*player)}
	w.housing = NewHousingIndex()
	plot := &housePlot{id: 7, characterID: 1, centerX: cx, centerY: cy}
	tx := int(math.Floor((cx + 40) / FarmTileSize))
	ty := int(math.Floor((cy + 30) / FarmTileSize))
	plot.crops = []farmTile{{tx: tx, ty: ty, crop: "carrot", stage: 5}}
	w.housing.byID[7] = plot
	w.housing.byCharacter[1] = plot
	px, py := tileCenter(tx, ty)
	w.players[1] = &player{id: 1, x: px - 12, y: py}

	res, msg, ok := w.ApplyFarmAction(1, "harvest", tx, ty, "", -1, 0)
	if !ok {
		t.Fatalf("harvest refused: %s", msg)
	}
	if res.PlayerAction != "" {
		t.Fatalf("harvest sent player action %q, want none", res.PlayerAction)
	}
}
