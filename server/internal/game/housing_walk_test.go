package game

import "testing"

// After entering, the player must be able to walk around the room and back out.
func TestInteriorSpawnCanMoveAndExit(t *testing.T) {
	const cx, cy = 500.0, 300.0
	sx, sy := HouseInteriorSpawn(cx, cy)

	walk := func(x, y, stepX, stepY float64, n int) (float64, float64) {
		for i := 0; i < n; i++ {
			x, y = resolveInteriorMove(x, y, x+stepX, y+stepY, cx, cy)
		}
		return x, y
	}

	if _, y := walk(sx, sy, 0, -2, 20); y > sy-30 {
		t.Fatalf("cannot walk north from the spawn: y moved %.1f", y-sy)
	}
	if x, _ := walk(sx, sy, 2, 0, 10); x < sx+15 {
		t.Fatalf("cannot walk east from the spawn: x moved %.1f", x-sx)
	}
	if x, _ := walk(sx, sy, -2, 0, 10); x > sx-15 {
		t.Fatalf("cannot walk west from the spawn: x moved %.1f", x-sx)
	}
	x, y := walk(sx, sy, 0, 2, 30)
	if !NearInteriorExit(x, y, cx, cy) {
		t.Fatalf("walking south from the spawn stops at (%.1f,%.1f), outside the exit zone", x-cx, y-cy)
	}
}

// The client asks to exit as soon as its predicted feet reach the exit zone, while the
// server position still trails a few pixels behind. That request must not be rejected.
func TestExitHouseAcceptsTrailingServerPosition(t *testing.T) {
	const cx, cy = 500.0, 300.0
	w := &World{players: make(map[int64]*player), housing: NewHousingIndex()}
	w.housing.byID[7] = &housePlot{id: 7, characterID: 1, centerX: cx, centerY: cy}
	sx, sy := HouseInteriorSpawn(cx, cy)
	w.AddPlayer(1, "Leaver", "human", PlayerAppearance{}, sx, sy, nil, 0, nil, "", PlayerVitals{})
	w.players[1].insideHouseID = 7

	if msg, ok := w.ExitHouse(1); !ok {
		t.Fatalf("exit from the interior spawn was rejected: %s", msg)
	}
}

func TestExitHouseRejectsFarFromDoor(t *testing.T) {
	const cx, cy = 500.0, 300.0
	w := &World{players: make(map[int64]*player), housing: NewHousingIndex()}
	w.housing.byID[7] = &housePlot{id: 7, characterID: 1, centerX: cx, centerY: cy}
	w.AddPlayer(1, "Stayer", "human", PlayerAppearance{}, cx, cy-60, nil, 0, nil, "", PlayerVitals{})
	w.players[1].insideHouseID = 7

	if _, ok := w.ExitHouse(1); ok {
		t.Fatal("exit from the far side of the room must be rejected")
	}
}

// Every farmable tile must lie fully inside the fence so tilled soil never pokes out.
func TestFarmTilesStayInsideFence(t *testing.T) {
	// House centres snap to tile centres.
	const cx, cy = 31*16 + 8.0, 19*16 + 8.0
	plot := &housePlot{centerX: cx, centerY: cy}
	minTx, maxTx := 1<<30, -(1 << 30)
	for tx := 20; tx < 45; tx++ {
		for ty := 8; ty < 32; ty++ {
			if !plot.tileFarmable(tx, ty) {
				continue
			}
			x0, y0 := float64(tx)*FarmTileSize, float64(ty)*FarmTileSize
			if x0 < cx-PlotHalfW || x0+FarmTileSize > cx+PlotHalfW ||
				y0 < cy-PlotHalfH || y0+FarmTileSize > cy+PlotHalfH {
				t.Fatalf("tile (%d,%d) crosses the fence", tx, ty)
			}
			minTx, maxTx = min(minTx, tx), max(maxTx, tx)
		}
	}
	if maxTx-minTx+1 != 7 {
		t.Fatalf("farmable columns = %d, want 7", maxTx-minTx+1)
	}
}

// Every room must be reachable from the hall through its doorway.
func TestInteriorDoorwaysConnectRooms(t *testing.T) {
	const cx, cy = 500.0, 300.0
	doorwayY := (interiorTileY(cy, interiorDoorwayTop) + interiorTileY(cy, interiorDoorwayBottom+1)) / 2
	// Feet sit below the collider, so aim the feet at the lower part of the doorway.
	feetY := doorwayY + 14
	walk := func(stepX float64) float64 {
		x, y := cx, feetY
		for i := 0; i < 200; i++ {
			x, y = resolveInteriorMove(x, y, x+stepX, y, cx, cy)
		}
		return x
	}
	if x := walk(-2); x > interiorTileX(cx, interiorDividerL) {
		t.Fatalf("walking west from the hall stopped at %.1f, before the bedroom", x-cx)
	}
	if x := walk(2); x < interiorTileX(cx, interiorDividerR+1) {
		t.Fatalf("walking east from the hall stopped at %.1f, before the kitchen", x-cx)
	}
}

func TestRemoveFurnitureNear(t *testing.T) {
	h := NewHousingIndex()
	plot := &housePlot{id: 7, characterID: 1, furniture: []FurnitureItem{
		{Type: "bed", X: 10, Y: 10}, {Type: "plant", X: 100, Y: 10},
	}}
	h.Add(plot)
	if h.RemoveFurnitureNear(1, 60, 10) {
		t.Fatal("a click between pieces must not remove anything")
	}
	if !h.RemoveFurnitureNear(1, 104, 14) {
		t.Fatal("a click on the plant should remove it")
	}
	if len(plot.furniture) != 1 || plot.furniture[0].Type != "bed" {
		t.Fatalf("furniture left = %+v, want just the bed", plot.furniture)
	}
}
