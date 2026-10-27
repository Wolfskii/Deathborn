package game

import (
	"math"
	"testing"
)

// The door threshold must sit on the cottage's door plate and inside the server's
// own approach zone, otherwise clicking the door is rejected ("Move closer to the door").
func TestHouseDoorThresholdIsReachable(t *testing.T) {
	const cx, cy = 500.0, 300.0
	dx, dy := HouseDoorPosition(cx, cy)

	if math.Abs(dx-cx-8.525) > 0.01 || dy != cy {
		t.Fatalf("door threshold = (%v,%v), want (%v,%v)", dx, dy, cx+8.525, cy)
	}
	if !NearHouseDoor(dx, dy, cx, cy) {
		t.Fatal("the door threshold itself should count as at the door")
	}
	if !inDoorApproach(dx, dy+6, cx, cy) {
		t.Fatal("standing just south of the threshold should be in the door approach")
	}
}

// A click on the door starts an enter from the client's interaction range, so the
// server must accept it too, or the door sound plays and the player never goes in.
func TestEnterHouseAcceptsClickFromInteractRange(t *testing.T) {
	const cx, cy = 500.0, 300.0
	w := &World{players: make(map[int64]*player), housing: NewHousingIndex()}
	w.housing.byID[7] = &housePlot{id: 7, characterID: 1, centerX: cx, centerY: cy}
	dx, dy := HouseDoorPosition(cx, cy)
	w.AddPlayer(1, "Enterer", "human", PlayerAppearance{}, dx, dy+60, nil, 0, nil, "", PlayerVitals{})

	if msg, ok := w.EnterHouse(1, 7); !ok {
		t.Fatalf("enter from 60px south of the door was rejected: %s", msg)
	}
	if got := w.players[1].insideHouseID; got != 7 {
		t.Fatalf("insideHouseID = %d, want 7", got)
	}
}

func TestEnterHouseRejectsFarAway(t *testing.T) {
	const cx, cy = 500.0, 300.0
	w := &World{players: make(map[int64]*player), housing: NewHousingIndex()}
	w.housing.byID[7] = &housePlot{id: 7, characterID: 1, centerX: cx, centerY: cy}
	w.AddPlayer(1, "Far", "human", PlayerAppearance{}, cx, cy+200, nil, 0, nil, "", PlayerVitals{})

	if _, ok := w.EnterHouse(1, 7); ok {
		t.Fatal("enter from 200px away must be rejected")
	}
}

// Walking north into the cottage stops in front of the wall, never inside it, and the stop
// point is still in the approach zone where E enters.
func TestWalkingNorthIntoDoorStopsOutsideWall(t *testing.T) {
	const cx, cy = 500.0, 300.0
	h := NewHousingIndex()
	h.byID[7] = &housePlot{id: 7, characterID: 1, centerX: cx, centerY: cy}
	dx, dy := HouseDoorPosition(cx, cy)

	for _, offsetX := range []float64{-1, 0, 4, 8} {
		p := &player{x: dx + offsetX, y: dy + 50, dirY: -1}
		for i := 0; i < 120; i++ {
			p.x, p.y = h.ResolveAgainstHouses(p.x, p.y, p.x, p.y-1.5)
		}
		// The feet collider touches the wall's bottom edge (cy+2) at feet cy+2+houseWallFootRy*2.
		if p.y < cy+houseBodyBottom+2*houseWallFootRy {
			t.Fatalf("walking north at door x%+v got inside the wall, stopped at (%.1f,%.1f)", offsetX, p.x-cx, p.y-cy)
		}
		if !inDoorApproach(p.x, p.y, cx, cy) {
			t.Fatalf("walking north at door x%+v stopped at (%.1f,%.1f), outside the E zone", offsetX, p.x-cx, p.y-cy)
		}
	}
}

// A ready crop in front of the door must still be harvestable: the tile may now be blocked
// for new planting, but a crop already in the ground is tended.
func TestHarvestCropInFrontOfDoor(t *testing.T) {
	const cx, cy = 500.0, 300.0
	w := &World{players: make(map[int64]*player)}
	w.housing = NewHousingIndex()
	plot := &housePlot{id: 7, characterID: 1, centerX: cx, centerY: cy}
	tx := int(math.Floor((cx + 15) / FarmTileSize))
	ty := int(math.Floor((cy + 19) / FarmTileSize))
	plot.crops = []farmTile{{tx: tx, ty: ty, crop: "carrot", stage: 5}}
	w.housing.byID[7] = plot
	w.housing.byCharacter[1] = plot
	px, py := tileCenter(tx, ty)
	w.players[1] = &player{id: 1, x: px - 12, y: py}

	if _, msg, ok := w.ApplyFarmAction(1, "harvest", tx, ty, "", -1, 0); !ok {
		t.Fatalf("harvest of a ready crop in front of the door was refused: %s", msg)
	}
}

// Soil in front of the door is farmable: only the cottage footprint is kept clear, so the
// player can till the tiles in front of the door.
func TestTilesInFrontOfDoorAreFarmable(t *testing.T) {
	const cx, cy = 500.0, 300.0
	p := &housePlot{id: 7, characterID: 1, centerX: cx, centerY: cy}
	found := false
	baseTx := int(math.Floor(cx / FarmTileSize))
	baseTy := int(math.Floor(cy / FarmTileSize))
	for ty := baseTy - 4; ty <= baseTy+8; ty++ {
		for tx := baseTx - 4; tx <= baseTx+4; tx++ {
			x, y := tileCenter(tx, ty)
			if inDoorApproach(x, y, cx, cy) && p.tileFarmable(tx, ty) {
				found = true
			}
		}
	}
	if !found {
		t.Fatal("no tile in front of the door is farmable")
	}
}
