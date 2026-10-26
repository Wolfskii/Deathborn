package game

// Homestead interior: 26 x 15 tiles of 16 px, three rooms side by side
// (bedroom | hall | kitchen). Mirrors client Gameplay/HouseInteriorLayout.cs.
// Tile (0,0) is the top-left corner of the interior (centre - InteriorHalfW/H).
//
//	row 0        dark wall top along the north edge
//	rows 1-2     wall face (wallpaper); the upper row is solid, players may stand in front of the lower one
//	rows 3-13    floor
//	row 14       south wall top, with the exit gap in columns 12-13
//	col 0 / 25   outer side walls
//	col 8 / 17   room dividers, open in rows 8-10 (doorways)
const (
	interiorTile          = 16.0
	interiorCols          = 26
	interiorRows          = 15
	interiorDividerL      = 8
	interiorDividerR      = 17
	interiorDoorwayTop    = 8
	interiorDoorwayBottom = 10
	interiorExitLeft      = 12
	interiorExitRight     = 13
	// interiorNorthSolidRows is how much of the north wall blocks movement: the wall top and the
	// upper wall-face row. The lower face row stays walkable so feet can reach the skirting board.
	interiorNorthSolidRows = 2
)

func interiorTileX(centerX float64, col int) float64 {
	return centerX - InteriorHalfW + float64(col)*interiorTile
}

func interiorTileY(centerY float64, row int) float64 {
	return centerY - InteriorHalfH + float64(row)*interiorTile
}

// interiorWallRects returns the solid interior walls as {left, right, top, bottom}.
func interiorWallRects(centerX, centerY float64) [][4]float64 {
	tx := func(c int) float64 { return interiorTileX(centerX, c) }
	ty := func(r int) float64 { return interiorTileY(centerY, r) }
	walls := [][4]float64{
		{tx(0), tx(interiorCols), ty(0), ty(interiorNorthSolidRows)},
		{tx(0), tx(1), ty(0), ty(interiorRows)},
		{tx(interiorCols - 1), tx(interiorCols), ty(0), ty(interiorRows)},
		{tx(0), tx(interiorExitLeft), ty(interiorRows - 1), ty(interiorRows)},
		{tx(interiorExitRight + 1), tx(interiorCols), ty(interiorRows - 1), ty(interiorRows)},
	}
	for _, div := range []int{interiorDividerL, interiorDividerR} {
		walls = append(walls,
			[4]float64{tx(div), tx(div + 1), ty(0), ty(interiorDoorwayTop)},
			[4]float64{tx(div), tx(div + 1), ty(interiorDoorwayBottom + 1), ty(interiorRows)},
		)
	}
	return walls
}

// feetHitWalls tests the player collision ellipse (same shape as exterior house collision
// and the client PlayerEntity collider) against the interior walls.
func feetHitWalls(x, y float64, walls [][4]float64) bool {
	const rx, ry = 6.0, 8.0
	cy := playerCollisionY(y, ry)
	for _, w := range walls {
		if ellipseOverlapsHouseRect(x, cy, rx, ry, w[0], w[1], w[2], w[3]) {
			return true
		}
	}
	return false
}
