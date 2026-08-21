package game

import "github.com/deathborn/server/internal/skills"

// player is the server-side mutable state for one character in the world.
type player struct {
	id         int64
	name       string
	race       string
	appearance PlayerAppearance
	x, y       float64
	hp         float64
	hpMax      float64
	// Client-reported resources mirrored for logout persistence (not simulated server-side).
	stamina float64
	mana    float64
	dead    bool
	hot     *healOverTime
	buffs   []playerBuff
	// desired movement direction (unit-clamped), set from client input and
	// integrated each tick.
	dirX, dirY              float64
	running                 bool
	skills                  skills.Set
	totalXp                 int64
	lastSkillInteract       float64
	inventory               []InventoryItem
	insideHouseID           int64
	headCosmetic            string
	houseTransitionCooldown float64
}

// PlayerState is the immutable view of a player included in snapshots sent to
// clients. JSON tags are the wire format.
type PlayerState struct {
	ID            int64            `json:"id"`
	Name          string           `json:"name"`
	X             float64          `json:"x"`
	Y             float64          `json:"y"`
	Hp            float64          `json:"hp"`
	HpMax         float64          `json:"hpMax"`
	InsideHouseID int64            `json:"insideHouseId,omitempty"`
	HeadCosmetic  string           `json:"headCosmetic,omitempty"`
	Race          string           `json:"race"`
	Appearance    PlayerAppearance `json:"appearance"`
}

// PlayerAppearance is the validated visual state shared with all clients.
type PlayerAppearance struct {
	Gender         string `json:"gender"`
	SkinTone       string `json:"skinTone"`
	EyeColor       string `json:"eyeColor"`
	HairStyleID    string `json:"hairStyleId"`
	SkinHue        int    `json:"skinHue"`
	SkinSaturation int    `json:"skinSaturation"`
	SkinBrightness int    `json:"skinBrightness"`
	EyeHue         int    `json:"eyeHue"`
	EyeSaturation  int    `json:"eyeSaturation"`
	EyeBrightness  int    `json:"eyeBrightness"`
	HairHue        int    `json:"hairHue"`
	HairSaturation int    `json:"hairSaturation"`
	HairBrightness int    `json:"hairBrightness"`
}
