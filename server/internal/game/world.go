// Package game holds the authoritative in-memory world state and the fixed-tick
// simulation. It has no knowledge of networking or persistence.
package game

import (
	"math"
	"sync"
)

// defaultSpeed is the max movement speed in pixels/second.
const defaultSpeed = 120.0

// World is the authoritative set of players and their positions. All access is
// guarded by a mutex so client read goroutines and the tick loop are safe.
type World struct {
	mu      sync.RWMutex
	players map[int64]*player
	speed   float64
}

func NewWorld() *World {
	return &World{
		players: make(map[int64]*player),
		speed:   defaultSpeed,
	}
}

// AddPlayer inserts a player at a position (e.g. on connect/spawn).
func (w *World) AddPlayer(id int64, name string, x, y float64) {
	w.mu.Lock()
	defer w.mu.Unlock()
	w.players[id] = &player{id: id, name: name, x: x, y: y}
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
		p.x += p.dirX * w.speed * dt
		p.y += p.dirY * w.speed * dt
	}
}

// Snapshot returns a copy of all player states for broadcasting.
func (w *World) Snapshot() []PlayerState {
	w.mu.RLock()
	defer w.mu.RUnlock()
	out := make([]PlayerState, 0, len(w.players))
	for _, p := range w.players {
		out = append(out, PlayerState{ID: p.id, Name: p.name, X: p.x, Y: p.y})
	}
	return out
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
