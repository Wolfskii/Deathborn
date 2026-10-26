package game

import "testing"

func TestDueRespawnsSplitsByClock(t *testing.T) {
	pending := []mobRespawn{
		{defID: "a", at: 10},
		{defID: "b", at: 50},
		{defID: "c", at: 30},
	}
	due, rest := dueRespawns(pending, 30)
	if len(due) != 2 || due[0].defID != "a" || due[1].defID != "c" {
		t.Fatalf("due = %+v, want a and c", due)
	}
	if len(rest) != 1 || rest[0].defID != "b" {
		t.Fatalf("rest = %+v, want b", rest)
	}
}

func TestStartingMobsDoNotRespawnWhenAllAreDead(t *testing.T) {
	w := &World{players: make(map[int64]*player)}
	w.ensureMobsInitialized()
	if len(w.mobMgr.mobs) == 0 {
		t.Fatal("first ensure should spawn the starting set")
	}
	for id := range w.mobMgr.mobs {
		delete(w.mobMgr.mobs, id)
	}
	w.ensureMobsInitialized()
	if len(w.mobMgr.mobs) != 0 {
		t.Fatalf("empty map must not re-spawn the starting set, got %d mobs", len(w.mobMgr.mobs))
	}
}

func TestKilledMobRespawnsAfterDelay(t *testing.T) {
	w := &World{players: make(map[int64]*player)}
	w.ensureMobsInitialized()

	var victim *mob
	for _, m := range w.mobMgr.mobs {
		if m.disposition != NpcFriendly {
			victim = m
			break
		}
	}
	if victim == nil {
		t.Fatal("no hostile mob to test with")
	}
	defID, sx, sy := victim.defID, victim.spawnX, victim.spawnY
	before := len(w.mobMgr.mobs)

	if _, _, died, ok := w.applyMobDamageLocked(victim.id, int(victim.hpMax)+1); !ok || !died {
		t.Fatalf("killing blow failed: ok=%v died=%v", ok, died)
	}
	if len(w.mobMgr.mobs) != before-1 {
		t.Fatal("killed mob should be removed")
	}

	w.mobMgr.clock = mobRespawnSeconds - 1
	w.processRespawnsLocked()
	if len(w.mobMgr.mobs) != before-1 {
		t.Fatal("mob must not respawn before its delay")
	}

	w.mobMgr.clock = mobRespawnSeconds
	w.processRespawnsLocked()
	if len(w.mobMgr.mobs) != before {
		t.Fatalf("mob should respawn after its delay: have %d, want %d", len(w.mobMgr.mobs), before)
	}
	var back bool
	for _, m := range w.mobMgr.mobs {
		if m.defID == defID && m.spawnX == sx && m.spawnY == sy {
			back = true
		}
	}
	if !back {
		t.Fatal("respawned mob should use the original spawn point")
	}
}

func TestRespawnWaitsWhenPlayerStandsNearby(t *testing.T) {
	w := &World{players: make(map[int64]*player)}
	w.mobMgr = newMobManager()
	w.mobMgr.initialized = true
	w.mobMgr.respawns = []mobRespawn{{defID: "forest_slime", x: 100, y: 100, at: 0}}
	w.players[1] = &player{id: 1, x: 110, y: 100}

	w.processRespawnsLocked()
	if len(w.mobMgr.mobs) != 0 {
		t.Fatal("no mob should spawn while a player is on the spawn point")
	}
	if len(w.mobMgr.respawns) != 1 || w.mobMgr.respawns[0].at != w.mobMgr.clock+mobRespawnRetrySeconds {
		t.Fatalf("respawn should be retried later, got %+v", w.mobMgr.respawns)
	}
}
