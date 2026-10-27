package game

import "testing"

func tickFor(p *housePlot, seconds, now float64) float64 {
	const dt = 0.5
	for elapsed := 0.0; elapsed < seconds; elapsed += dt {
		now += dt
		p.tickFarm(dt, now)
	}
	return now
}

// One watering grows a crop exactly one stage; then the soil dries and growth waits.
func TestWateringGrowsOneStageThenDries(t *testing.T) {
	d := cropDefs["parsnip"]
	now := 1000.0
	p := &housePlot{crops: []farmTile{{tx: 1, ty: 1, crop: "parsnip"}}}

	now = tickFor(p, d.stageSec*2, now)
	if p.crops[0].stage != 0 {
		t.Fatalf("dry soil grew the crop to stage %d", p.crops[0].stage)
	}
	if !p.crops[0].thirsty(now) {
		t.Fatal("a planted crop in dry soil should be thirsty")
	}

	p.crops[0].wateredUntil = now + FarmWaterDuration
	now = tickFor(p, d.stageSec*3, now)
	if p.crops[0].stage != 1 {
		t.Fatalf("one watering grew to stage %d, want 1", p.crops[0].stage)
	}
	if p.crops[0].wateredAt(now) {
		t.Fatal("soil should dry out once the stage completes")
	}
}

func TestDryCropWiltsAndWateringRevives(t *testing.T) {
	now := 1000.0
	p := &housePlot{crops: []farmTile{{tx: 1, ty: 1, crop: "carrot"}}}
	now = tickFor(p, FarmWiltAfter+1, now)
	if !p.crops[0].wilted(now) {
		t.Fatal("crop should wilt after standing dry")
	}
	if p.crops[0].crop != "carrot" {
		t.Fatal("wilting must not kill the crop")
	}

	p.crops[0].wateredUntil = now + FarmWaterDuration
	now = tickFor(p, 1, now)
	if p.crops[0].wilted(now) || p.crops[0].dryFor != 0 {
		t.Fatalf("watering should revive the crop (dryFor=%v)", p.crops[0].dryFor)
	}
}

func TestReadyCropIsNeverThirsty(t *testing.T) {
	d := cropDefs["parsnip"]
	now := 1000.0
	p := &housePlot{crops: []farmTile{{tx: 1, ty: 1, crop: "parsnip", stage: d.stages - 1}}}
	now = tickFor(p, FarmWiltAfter+10, now)
	if p.crops[0].thirsty(now) || p.crops[0].wilted(now) {
		t.Fatal("a crop ready to harvest should not ask for water")
	}
}
