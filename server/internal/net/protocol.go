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

// AbilityHitNpcSendData is sent when a player hits an NPC/boss.
type AbilityHitNpcSendData struct {
	TargetNpcID int64  `json:"targetNpcId"`
	Damage      int    `json:"damage"`
	Ability     string `json:"ability"`
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

// NpcHitData is broadcast when an NPC/boss is hit.
type NpcHitData struct {
	AttackerID  int64   `json:"attackerId"`
	TargetNpcID int64   `json:"targetNpcId"`
	Damage      int     `json:"damage"`
	Ability     string  `json:"ability"`
	Hp          float64 `json:"hp"`
	HpMax       float64 `json:"hpMax"`
}

// BossSpawnData announces a world boss spawn.
type BossSpawnData struct {
	NpcID int64   `json:"npcId"`
	DefID string  `json:"defId"`
	Name  string  `json:"name"`
	X     float64 `json:"x"`
	Y     float64 `json:"y"`
}

// BossDeathData announces a world boss death.
type BossDeathData struct {
	NpcID int64   `json:"npcId"`
	DefID string  `json:"defId"`
	Name  string  `json:"name"`
	X     float64 `json:"x"`
	Y     float64 `json:"y"`
}

// WorldEventData announces world boss event state (PvP toggle).
type WorldEventData struct {
	Active  bool   `json:"active"`
	Name    string `json:"name,omitempty"`
	PvPOff  bool   `json:"pvpOff"`
	BossCnt int    `json:"bossCount"`
}

// BossActionData is broadcast when a boss uses a visible ability.
type BossActionData struct {
	NpcID  int64  `json:"npcId"`
	Action string `json:"action"`
	X      float64 `json:"x"`
	Y      float64 `json:"y"`
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
	CharacterID int64              `json:"characterId"`
	X           float64            `json:"x"`
	Y           float64            `json:"y"`
	Name        string             `json:"name"`
	Skills      map[string]int64   `json:"skills,omitempty"`
	TotalXp     int64              `json:"totalXp,omitempty"`
	Inventory   []game.InventoryItem `json:"inventory,omitempty"`
}

// InventoryData syncs a player's inventory to the client.
type InventoryData struct {
	Items []game.InventoryItem `json:"items"`
}

// PickupItemSendData requests picking up a ground loot pile.
type PickupItemSendData struct {
	DropID int64 `json:"dropId"`
}

// WorldItemRemovedData is broadcast when ground loot is picked up.
type WorldItemRemovedData struct {
	DropID int64 `json:"dropId"`
}

// SkillXpGainData is sent when a player gains skill XP.
type SkillXpGainData struct {
	PlayerID  int64  `json:"playerId"`
	SkillID   string `json:"skillId"`
	Amount    int64  `json:"amount"`
	Xp        int64  `json:"xp"`
	Level     int    `json:"level"`
	LeveledUp bool   `json:"leveledUp"`
	TotalXp   int64  `json:"totalXp"`
	Hp        float64 `json:"hp,omitempty"`
	HpMax     float64 `json:"hpMax,omitempty"`
}

// BuildHouseSendData requests placing a house at the player's position or coords.
type BuildHouseSendData struct {
	X *float64 `json:"x,omitempty"`
	Y *float64 `json:"y,omitempty"`
}

// PlaceFurnitureSendData places one furniture item in the owner's house.
type PlaceFurnitureSendData struct {
	Type string  `json:"type"`
	X    float64 `json:"x"`
	Y    float64 `json:"y"`
}

// HouseEnterSendData requests entering a house through its door.
type HouseEnterSendData struct {
	HouseID int64 `json:"houseId"`
}

// HouseExitSendData requests leaving the current house interior.
type HouseExitSendData struct{}

// HouseBuiltData is broadcast when a house is created.
type HouseBuiltData struct {
	House game.HouseState `json:"house"`
}

// HouseRemovedData is broadcast when a house is removed.
type HouseRemovedData struct {
	HouseID   int64 `json:"houseId"`
	OwnerID   int64 `json:"ownerId"`
}

// HouseUpdatedData is broadcast when furniture changes.
type HouseUpdatedData struct {
	House game.HouseState `json:"house"`
}
type SnapshotData struct {
	Tick       uint64                    `json:"tick"`
	Players    []game.PlayerState        `json:"players"`
	Npcs       []game.NpcState           `json:"npcs,omitempty"`
	Houses     []game.HouseState         `json:"houses,omitempty"`
	WorldItems []game.WorldItemDropState `json:"worldItems,omitempty"`
	WorldEvent *game.WorldEventState     `json:"worldEvent,omitempty"`
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
func BuildSnapshot(tick uint64, players []game.PlayerState, npcs []game.NpcState, houses []game.HouseState, worldItems []game.WorldItemDropState, worldEvent *game.WorldEventState) []byte {
	return encode("snapshot", SnapshotData{Tick: tick, Players: players, Npcs: npcs, Houses: houses, WorldItems: worldItems, WorldEvent: worldEvent})
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

// BuildSkillXpGain serializes a skill XP gain for the affected player.
func BuildSkillXpGain(d SkillXpGainData) []byte {
	return encode("skill_xp_gain", d)
}

func BuildNpcHit(attackerID, targetNpcID int64, damage int, ability string, hp, hpMax float64) []byte {
	return encode("npc_hit", NpcHitData{
		AttackerID: attackerID, TargetNpcID: targetNpcID,
		Damage: damage, Ability: ability, Hp: hp, HpMax: hpMax,
	})
}

func BuildBossSpawn(npcID int64, defID, name string, x, y float64) []byte {
	return encode("boss_spawn", BossSpawnData{NpcID: npcID, DefID: defID, Name: name, X: x, Y: y})
}

func BuildBossDeath(npcID int64, defID, name string, x, y float64) []byte {
	return encode("boss_death", BossDeathData{NpcID: npcID, DefID: defID, Name: name, X: x, Y: y})
}

func BuildBossAction(npcID int64, action string, x, y float64) []byte {
	return encode("boss_action", BossActionData{NpcID: npcID, Action: action, X: x, Y: y})
}

func BuildWorldEvent(d WorldEventData) []byte {
	return encode("world_event", d)
}

func BuildHouseBuilt(h game.HouseState) []byte {
	return encode("house_built", HouseBuiltData{House: h})
}

func BuildHouseRemoved(houseID, ownerID int64) []byte {
	return encode("house_removed", HouseRemovedData{HouseID: houseID, OwnerID: ownerID})
}

func BuildHouseUpdated(h game.HouseState) []byte {
	return encode("house_updated", HouseUpdatedData{House: h})
}

func BuildInventory(items []game.InventoryItem) []byte {
	return encode("inventory", InventoryData{Items: items})
}

func BuildWorldItemRemoved(dropID int64) []byte {
	return encode("world_item_removed", WorldItemRemovedData{DropID: dropID})
}
