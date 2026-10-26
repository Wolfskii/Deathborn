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
