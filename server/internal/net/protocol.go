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
	DirX    float64 `json:"dirX"`
	DirY    float64 `json:"dirY"`
	Running bool    `json:"running,omitempty"`
}

// CreateCharacterData is sent on the "create_character" message.
type CreateCharacterData struct {
	Name string `json:"name"`
}

// InteractData is sent when the player interacts with a world object.
type InteractData struct {
	TargetID string `json:"targetId"`
}

// CastFireballData is sent when the player casts a fireball.
type CastFireballData struct {
	DirX float64 `json:"dirX"`
	DirY float64 `json:"dirY"`
}

// CastSpellData is sent when the player casts a spell from the hotbar.
type CastSpellData struct {
	SpellID string  `json:"spellId"`
	DirX    float64 `json:"dirX"`
	DirY    float64 `json:"dirY"`
}

// PlayerActionSendData is sent when the player performs a visible action.
type PlayerActionSendData struct {
	Action   string  `json:"action"`
	DirX     float64 `json:"dirX,omitempty"`
	DirY     float64 `json:"dirY,omitempty"`
	TargetID string  `json:"targetId,omitempty"`
}

// PlayerActionData is broadcast when any player performs a visible action.
type PlayerActionData struct {
	PlayerID int64   `json:"playerId"`
	Action   string  `json:"action"`
	DirX     float64 `json:"dirX,omitempty"`
	DirY     float64 `json:"dirY,omitempty"`
	TargetID string  `json:"targetId,omitempty"`
}

// ProjectileSpawnData is broadcast when a projectile is created.
type ProjectileSpawnData struct {
	OwnerID int64   `json:"ownerId"`
	SpellID string  `json:"spellId,omitempty"`
	X       float64 `json:"x"`
	Y       float64 `json:"y"`
	DirX    float64 `json:"dirX"`
	DirY    float64 `json:"dirY"`
}

// SpellEffectSpawnData is broadcast when a ground spell effect is created.
type SpellEffectSpawnData struct {
	OwnerID int64   `json:"ownerId"`
	SpellID string  `json:"spellId"`
	X       float64 `json:"x"`
	Y       float64 `json:"y"`
	DirX    float64 `json:"dirX"`
	DirY    float64 `json:"dirY"`
}

// ChatMessageSendData is sent when a player posts chat.
type ChatMessageSendData struct {
	Text string `json:"text"`
}

// ChatMessageData is broadcast when a player posts chat.
type ChatMessageData struct {
	PlayerID int64  `json:"playerId"`
	Text     string `json:"text"`
}

// ChatTypingSendData is sent when a player opens/closes the chat composer.
type ChatTypingSendData struct {
	Typing bool `json:"typing"`
}

// ChatTypingData is broadcast when a player's typing indicator changes.
type ChatTypingData struct {
	PlayerID int64 `json:"playerId"`
	Typing   bool  `json:"typing"`
}

// AbilityUseSendData is sent when a player uses a self-target ability or item.
type AbilityUseSendData struct {
	Ability string `json:"ability"`
}

// AbilityHitSendData is sent when a player hits another with a damaging ability.
type AbilityHitSendData struct {
	TargetID int64  `json:"targetId"`
	Damage   int    `json:"damage"`
	Ability  string `json:"ability"`
}

// PlayerHitData is broadcast when a player is hit by an ability.
type PlayerHitData struct {
	AttackerID int64   `json:"attackerId"`
	TargetID   int64   `json:"targetId"`
	Damage     int     `json:"damage"`
	Ability    string  `json:"ability"`
	Hp         float64 `json:"hp"`
	HpMax      float64 `json:"hpMax"`
}

// PlayerDeathData is broadcast when a player dies.
type PlayerDeathData struct {
	PlayerID   int64   `json:"playerId"`
	KillerID   int64   `json:"killerId,omitempty"`
	X          float64 `json:"x"`
	Y          float64 `json:"y"`
	DirX       float64 `json:"dirX"`
	DirY       float64 `json:"dirY"`
}

// YouDiedData tells the victim their character is dead and they may create a new one.
type YouDiedData struct {
	X    float64 `json:"x"`
	Y    float64 `json:"y"`
	DirX float64 `json:"dirX"`
	DirY float64 `json:"dirY"`
}

// PlayerHealData is broadcast when a player heals themselves.
type PlayerHealData struct {
	PlayerID int64   `json:"playerId"`
	Amount   int     `json:"amount"`
	Ability  string  `json:"ability"`
	Hp       float64 `json:"hp"`
	HpMax    float64 `json:"hpMax"`
}

// PlayerBuffData is broadcast when a buff is applied or expires (duration 0 = expired).
type PlayerBuffData struct {
	PlayerID     int64   `json:"playerId"`
	BuffID       string  `json:"buffId"`
	Duration     float64 `json:"duration"`
	MarkTargetID int64   `json:"markTargetId,omitempty"`
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

// BuildPlayerDeath serializes a player death for broadcast.
func BuildPlayerDeath(playerID, killerID int64, x, y, dirX, dirY float64) []byte {
	return encode("player_death", PlayerDeathData{
		PlayerID: playerID,
		KillerID: killerID,
		X:        x,
		Y:        y,
		DirX:     dirX,
		DirY:     dirY,
	})
}

// BuildYouDied serializes the victim-only death notice.
func BuildYouDied(x, y, dirX, dirY float64) []byte {
	return encode("you_died", YouDiedData{X: x, Y: y, DirX: dirX, DirY: dirY})
}

// BuildPlayerHeal serializes a heal event for broadcast.
func BuildPlayerHeal(playerID int64, amount int, ability string, hp, hpMax float64) []byte {
	return encode("player_heal", PlayerHealData{
		PlayerID: playerID,
		Amount:   amount,
		Ability:  ability,
		Hp:       hp,
		HpMax:    hpMax,
	})
}

// BuildPlayerBuff serializes a buff apply/expire event for broadcast.
func BuildPlayerBuff(playerID int64, buffID string, duration float64, markTargetID int64) []byte {
	return encode("player_buff", PlayerBuffData{
		PlayerID:     playerID,
		BuffID:       buffID,
		Duration:     duration,
		MarkTargetID: markTargetID,
	})
}

// BuildSnapshot serializes a world snapshot for broadcast.
func BuildSnapshot(tick uint64, players []game.PlayerState) []byte {
	return encode("snapshot", SnapshotData{Tick: tick, Players: players})
}

// BuildPlayerAction serializes a player action for broadcast.
func BuildPlayerAction(playerID int64, action string, dirX, dirY float64, targetID string) []byte {
	return encode("player_action", PlayerActionData{
		PlayerID: playerID,
		Action:   action,
		DirX:     dirX,
		DirY:     dirY,
		TargetID: targetID,
	})
}
