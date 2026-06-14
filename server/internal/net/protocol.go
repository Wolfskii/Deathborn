package net

import (
	"encoding/json"

	"github.com/deathborn/server/internal/game"
)

// Envelope is the outer JSON wrapper for every message in both directions:
// {"type": "...", "data": {...}}.
type Envelope struct {
	Type string          `json:"type"`
	Data json.RawMessage `json:"data"`
}

// --- Client -> Server ---

// InputData is the player's desired movement direction (unit vector-ish; the
// server clamps it). Sent on the "input" message.
type InputData struct {
	DirX float64 `json:"dirX"`
	DirY float64 `json:"dirY"`
}

// CreateCharacterData is sent on the "create_character" message.
type CreateCharacterData struct {
	Name string `json:"name"`
}

// InteractData is sent when the player interacts with a world object.
type InteractData struct {
	TargetID string `json:"targetId"`
}

// --- Server -> Client ---

// WelcomeData tells the client which entity id is theirs and the spawn point.
type WelcomeData struct {
	CharacterID int64   `json:"characterId"`
	X           float64 `json:"x"`
	Y           float64 `json:"y"`
	Name        string  `json:"name"`
}

// SnapshotData is the authoritative world state broadcast each tick.
type SnapshotData struct {
	Tick    uint64             `json:"tick"`
	Players []game.PlayerState `json:"players"`
}

// MessageData carries a human-readable message ("error", "need_character").
type MessageData struct {
	Message string `json:"message"`
}

// encode marshals a typed payload into an envelope of the given type.
func encode(typ string, data any) []byte {
	raw, _ := json.Marshal(data)
	env, _ := json.Marshal(Envelope{Type: typ, Data: raw})
	return env
}

// BuildSnapshot serializes a world snapshot for broadcast.
func BuildSnapshot(tick uint64, players []game.PlayerState) []byte {
	return encode("snapshot", SnapshotData{Tick: tick, Players: players})
}
