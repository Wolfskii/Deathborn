package game

import "testing"

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
