package game

import "testing"

func newDashWorld() *World {
	w := &World{players: make(map[int64]*player)}
	w.players[1] = &player{id: 1, x: 100, y: 100, dirX: 0, dirY: 1}
	return w
}

// A charge must not leave the player walking in the charge direction afterwards.
func TestDashKeepsMovementInput(t *testing.T) {
	w := newDashWorld()
	if _, _, ok := w.DashPlayer(1, 1, 0, 80); !ok {
		t.Fatal("dash failed")
	}
	p := w.players[1]
	if p.dirX != 0 || p.dirY != 1 {
		t.Fatalf("dash changed movement input to (%v,%v), want unchanged (0,1)", p.dirX, p.dirY)
	}
	if p.x != 180 || p.y != 100 {
		t.Fatalf("dash landed at (%v,%v), want (180,100)", p.x, p.y)
	}
}

func TestAdoptDashStartWithinDrift(t *testing.T) {
	w := newDashWorld()
	if !w.AdoptDashStart(1, 104, 97, dashStartMaxDriftForTest) {
		t.Fatal("a start a few pixels away should be accepted")
	}
	if p := w.players[1]; p.x != 104 || p.y != 97 {
		t.Fatalf("position = (%v,%v), want the client start (104,97)", p.x, p.y)
	}
}

func TestAdoptDashStartRejectsFarStart(t *testing.T) {
	w := newDashWorld()
	if w.AdoptDashStart(1, 400, 400, dashStartMaxDriftForTest) {
		t.Fatal("a start far from the server position must be rejected")
	}
	if p := w.players[1]; p.x != 100 || p.y != 100 {
		t.Fatalf("rejected start moved the player to (%v,%v)", p.x, p.y)
	}
}

const dashStartMaxDriftForTest = 48.0
