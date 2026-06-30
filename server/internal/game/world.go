// Package game holds the authoritative in-memory world state and the fixed-tick
// simulation. It has no knowledge of networking or persistence.
package game

import (
	"math"
	"sync"

	"github.com/deathborn/server/internal/worldmap"
)

// defaultSpeed is the max movement speed in pixels/second.
const defaultSpeed = 120.0

// World is the authoritative set of players and their positions. All access is
// guarded by a mutex so client read goroutines and the tick loop are safe.
type World struct {
	mu      sync.RWMutex
	players map[int64]*player
	speed   float64
	terrain *worldmap.Map
}

func NewWorld(terrain *worldmap.Map) *World {
	return &World{
		players: make(map[int64]*player),
		speed:   defaultSpeed,
		terrain: terrain,
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

// RemovePlayer removes a player (e.g. on disconnect).
func (w *World) RemovePlayer(id int64) {
	w.mu.Lock()
	defer w.mu.Unlock()
	delete(w.players, id)
}

// SetInput records a player's desired movement direction. The vector is clamped
// to unit length so clients cannot move faster by sending a large vector.
func (w *World) SetInput(id int64, dirX, dirY float64) {
	w.mu.Lock()
	defer w.mu.Unlock()
	p, ok := w.players[id]
	if !ok {
		return
	}
	if l := math.Hypot(dirX, dirY); l > 1 {
		dirX /= l
		dirY /= l
	}
	p.dirX, p.dirY = dirX, dirY
}

// Step advances the simulation by dt seconds, integrating each player's
// velocity from its input direction.
func (w *World) Step(dt float64) {
	w.mu.Lock()
	defer w.mu.Unlock()
	for _, p := range w.players {
		dx := p.dirX * w.speed * dt
		dy := p.dirY * w.speed * dt
		if w.terrain != nil {
			p.x, p.y = w.terrain.ResolveMove(p.x, p.y, dx, dy)
		} else {
			p.x += dx
			p.y += dy
		}
	}
}

// Snapshot returns a copy of all player states for broadcasting.
func (w *World) Snapshot() []PlayerState {
	w.mu.RLock()
	defer w.mu.RUnlock()
	out := make([]PlayerState, 0, len(w.players))
	for _, p := range w.players {
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

// ApplyDamage reduces a player's HP by damage. Returns the new HP values.
func (w *World) ApplyDamage(targetID int64, damage int) (hp, hpMax float64, ok bool) {
	if damage <= 0 {
		return 0, 0, false
	}
	w.mu.Lock()
	defer w.mu.Unlock()
	p, ok := w.players[targetID]
	if !ok {
		return 0, 0, false
	}
	p.hp -= float64(damage)
	if p.hp < 0 {
		p.hp = 0
	}
	return p.hp, p.hpMax, true
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
	if !ok {
		return 0, 0, false
	}
	return p.x, p.y, true
}
