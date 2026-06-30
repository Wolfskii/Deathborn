// Package game holds the authoritative in-memory world state and the fixed-tick
// simulation. It has no knowledge of networking or persistence.
package game

import (
	"math"
	"sync"

	"github.com/deathborn/server/internal/worldmap"
)

// defaultSpeed is walk speed in pixels/second.
const defaultSpeed = 120.0
const runSpeed = 195.0

// World is the authoritative set of players and their positions. All access is
// guarded by a mutex so client read goroutines and the tick loop are safe.
type World struct {
	mu       sync.RWMutex
	players  map[int64]*player
	speed    float64
	runSpeed float64
	terrain  *worldmap.Map
}

func NewWorld(terrain *worldmap.Map) *World {
	return &World{
		players:  make(map[int64]*player),
		speed:    defaultSpeed,
		runSpeed: runSpeed,
		terrain:  terrain,
	}
}

// AddPlayer inserts a player at a position (e.g. on connect/spawn).
func (w *World) AddPlayer(id int64, name string, x, y float64) {
	w.mu.Lock()
	defer w.mu.Unlock()
	w.players[id] = &player{
		id: id, name: name, x: x, y: y,
		hp: DefaultHpMax, hpMax: DefaultHpMax,
	}
}

// DeathPose returns a dead player's position and facing for the death broadcast.
func (w *World) DeathPose(id int64) (x, y, dirX, dirY float64, ok bool) {
	w.mu.RLock()
	defer w.mu.RUnlock()
	p, ok := w.players[id]
	if !ok {
		return 0, 0, 0, 0, false
	}
	dirX, dirY = p.dirX, p.dirY
	if dirX == 0 && dirY == 0 {
		dirY = 1
	}
	return p.x, p.y, dirX, dirY, true
}

// RemovePlayer removes a player (e.g. on disconnect or death).
func (w *World) RemovePlayer(id int64) {
	w.mu.Lock()
	defer w.mu.Unlock()
	delete(w.players, id)
}

// SetInput records a player's desired movement direction and run state.
func (w *World) SetInput(id int64, dirX, dirY float64, running bool) {
	w.mu.Lock()
	defer w.mu.Unlock()
	p, ok := w.players[id]
	if !ok || p.dead {
		return
	}
	if l := math.Hypot(dirX, dirY); l > 1 {
		dirX /= l
		dirY /= l
	}
	p.dirX, p.dirY = dirX, dirY
	p.running = running && (dirX != 0 || dirY != 0)
}

// Step advances the simulation by dt seconds. Returns heal-over-time events.
func (w *World) Step(dt float64) []HealEvent {
	w.mu.Lock()
	defer w.mu.Unlock()

	var heals []HealEvent
	for _, p := range w.players {
		if p.dead {
			continue
		}
		dx := p.dirX * w.speed * dt
		dy := p.dirY * w.speed * dt
		if p.running {
			dx = p.dirX * w.runSpeed * dt
			dy = p.dirY * w.runSpeed * dt
		}
		if w.terrain != nil {
			p.x, p.y = w.terrain.ResolveMove(p.x, p.y, dx, dy)
		} else {
			p.x += dx
			p.y += dy
		}

		if p.hot == nil {
			continue
		}
		p.hot.accum += dt
		for p.hot.accum >= p.hot.interval && p.hot.ticksLeft > 0 {
			p.hot.accum -= p.hot.interval
			p.hot.ticksLeft--
			p.hp += float64(p.hot.perTick)
			if p.hp > p.hpMax {
				p.hp = p.hpMax
			}
			heals = append(heals, HealEvent{
				PlayerID: p.id,
				Amount:   p.hot.perTick,
				Ability:  p.hot.ability,
				Hp:       p.hp,
				HpMax:    p.hpMax,
			})
		}
		if p.hot.ticksLeft <= 0 {
			p.hot = nil
		}
	}
	return heals
}

// TickBuffs advances buff timers. Call once per simulation tick after Step.
func (w *World) TickBuffs(dt float64) []BuffEvent {
	return w.tickPlayerBuffs(dt)
}

// DashPlayer moves a player forward along dir, respecting terrain collision.
func (w *World) DashPlayer(id int64, dirX, dirY, distance float64) (newX, newY float64, ok bool) {
	w.mu.Lock()
	defer w.mu.Unlock()
	p, ok := w.players[id]
	if !ok || p.dead {
		return 0, 0, false
	}
	if l := math.Hypot(dirX, dirY); l > 0.01 {
		dirX /= l
		dirY /= l
	} else {
		dirX, dirY = 0, 1
	}
	p.dirX, p.dirY = dirX, dirY
	dx := dirX * distance
	dy := dirY * distance
	if w.terrain != nil {
		p.x, p.y = w.terrain.ResolveMove(p.x, p.y, dx, dy)
	} else {
		p.x += dx
		p.y += dy
	}
	return p.x, p.y, true
}

// Snapshot returns a copy of all player states for broadcasting.
func (w *World) Snapshot() []PlayerState {
	w.mu.RLock()
	defer w.mu.RUnlock()
	out := make([]PlayerState, 0, len(w.players))
	for _, p := range w.players {
		if p.dead {
			continue
		}
		out = append(out, PlayerState{
			ID: p.id, Name: p.name, X: p.x, Y: p.y,
			Hp: p.hp, HpMax: p.hpMax,
		})
	}
	return out
}

// DefaultSpawn returns the preferred starter position on walkable land.
func (w *World) DefaultSpawn() (float64, float64) {
	if w.terrain != nil {
		return w.terrain.DefaultSpawnX, w.terrain.DefaultSpawnY
	}
	return 0, 0
}

// CanWalk reports whether a player-sized circle may stand at (x,y).
func (w *World) CanWalk(x, y float64) bool {
	if w.terrain == nil {
		return true
	}
	return w.terrain.CanWalk(x, y, 12)
}

// StartBandageHoT begins bandage healing over time. Returns false if already active.
func (w *World) StartBandageHoT(playerID int64) bool {
	w.mu.Lock()
	defer w.mu.Unlock()
	p, ok := w.players[playerID]
	if !ok || p.hot != nil {
		return false
	}
	p.hot = &healOverTime{
		ability:   "bandage",
		interval:  BandageHoTInterval,
		perTick:   BandageHoTPerTick(),
		ticksLeft: BandageHoTTicks,
	}
	return true
}

// ValidateAbilityHit checks attacker/target distance for a reported hit.
func (w *World) ValidateAbilityHit(attackerID, targetID int64, ability string) bool {
	maxR := MaxHitRange(ability)
	if maxR <= 0 {
		return false
	}
	w.mu.RLock()
	defer w.mu.RUnlock()
	a, okA := w.players[attackerID]
	t, okT := w.players[targetID]
	if !okA || !okT {
		return false
	}
	dx := a.x - t.x
	dy := a.y - t.y
	return dx*dx+dy*dy <= maxR*maxR
}

// ApplyDamage reduces a player's HP by damage. Returns the new HP values and whether they just died.
func (w *World) ApplyDamage(targetID int64, damage int) (hp, hpMax float64, justDied bool, ok bool) {
	if damage <= 0 {
		return 0, 0, false, false
	}
	w.mu.Lock()
	defer w.mu.Unlock()
	p, ok := w.players[targetID]
	if !ok || p.dead {
		return 0, 0, false, false
	}
	p.hp -= float64(damage)
	if p.hp <= 0 {
		p.hp = 0
		if !p.dead {
			p.dead = true
			justDied = true
		}
	}
	return p.hp, p.hpMax, justDied, true
}

// ApplyHeal increases a player's HP up to their maximum.
func (w *World) ApplyHeal(playerID int64, amount int) (hp, hpMax float64, ok bool) {
	if amount <= 0 {
		return 0, 0, false
	}
	w.mu.Lock()
	defer w.mu.Unlock()
	p, ok := w.players[playerID]
	if !ok {
		return 0, 0, false
	}
	p.hp += float64(amount)
	if p.hp > p.hpMax {
		p.hp = p.hpMax
	}
	return p.hp, p.hpMax, true
}

// Position returns a player's current position, if present.
func (w *World) Position(id int64) (x, y float64, ok bool) {
	w.mu.RLock()
	defer w.mu.RUnlock()
	p, ok := w.players[id]
	if !ok || p.dead {
		return 0, 0, false
	}
	return p.x, p.y, true
}
