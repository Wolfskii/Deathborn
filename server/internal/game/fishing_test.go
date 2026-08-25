package game

import "testing"

func TestFishingStarterKitHasRod(t *testing.T) {
	kit := FishingStarterKit()
	hasRod, hasBait := false, false
	for _, it := range kit {
		if it.ItemID == "fishing_rod" && it.Count == 1 {
			hasRod = true
		}
		if it.ItemID == "worm_bait" && it.Count > 0 {
			hasBait = true
		}
	}
	if !hasRod || !hasBait {
		t.Fatalf("kit missing rod or bait: %+v", kit)
	}
}

func TestRollFishStaysInWaterKind(t *testing.T) {
	for i := 0; i < 80; i++ {
		d := rollFish(1, false, false)
		if d.sea {
			t.Fatalf("river roll returned sea fish %s", d.id)
		}
		d = rollFish(1, true, false)
		if !d.sea {
			t.Fatalf("sea roll returned river fish %s", d.id)
		}
	}
}

func TestRollFishHighLevelCanCatchRare(t *testing.T) {
	seenRare := false
	for i := 0; i < 400; i++ {
		d := rollFish(99, false, true)
		if d.id == "golden_fish" {
			seenRare = true
			break
		}
	}
	if !seenRare {
		t.Fatal("expected baited high-level river rolls to include golden_fish")
	}
}

func TestFishingItemStacks(t *testing.T) {
	if FishingItemMaxStack("fishing_rod") != 1 {
		t.Fatal("rod should not stack")
	}
	if FishingItemMaxStack("carp") != 40 {
		t.Fatal("fish should stack")
	}
	if !IsFishItem("tuna") || IsFishItem("hoe") {
		t.Fatal("fish item detection")
	}
}
