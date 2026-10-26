package game

import "testing"

func TestClaimDeathOnlyOnce(t *testing.T) {
	w := &World{players: make(map[int64]*player)}
	w.players[1] = &player{id: 1, x: 10, y: 20, dead: true}

	if _, _, _, _, ok := w.ClaimDeath(1); !ok {
		t.Fatal("first claim of a dead player should succeed")
	}
	if _, _, _, _, ok := w.ClaimDeath(1); ok {
		t.Fatal("second claim must fail so the death is handled once")
	}
}

func TestClaimDeathRequiresDeadPlayer(t *testing.T) {
	w := &World{players: make(map[int64]*player)}
	w.players[2] = &player{id: 2, x: 1, y: 1, dead: false}

	if _, _, _, _, ok := w.ClaimDeath(2); ok {
		t.Fatal("living player must not be claimed")
	}
	if _, _, _, _, ok := w.ClaimDeath(99); ok {
		t.Fatal("missing player must not be claimed")
	}
}

func TestIsDead(t *testing.T) {
	w := &World{players: make(map[int64]*player)}
	w.players[3] = &player{id: 3, dead: true}
	w.players[4] = &player{id: 4}

	if !w.IsDead(3) {
		t.Fatal("dead player should report dead")
	}
	if w.IsDead(4) || w.IsDead(99) {
		t.Fatal("living or missing player should not report dead")
	}
}

func TestClaimDeathDefaultsFacingDown(t *testing.T) {
	w := &World{players: make(map[int64]*player)}
	w.players[5] = &player{id: 5, dead: true}

	_, _, dirX, dirY, ok := w.ClaimDeath(5)
	if !ok || dirX != 0 || dirY != 1 {
		t.Fatalf("got dir (%v,%v) ok=%v, want (0,1) ok=true", dirX, dirY, ok)
	}
}
