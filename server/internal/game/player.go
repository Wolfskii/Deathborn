package game

// player is the server-side mutable state for one character in the world.
type player struct {
	id   int64
	name string
	x, y float64
	hp    float64
	hpMax float64
	hot   *healOverTime
	// desired movement direction (unit-clamped), set from client input and
	// integrated each tick.
	dirX, dirY float64
}

// PlayerState is the immutable view of a player included in snapshots sent to
// clients. JSON tags are the wire format.
type PlayerState struct {
	ID    int64   `json:"id"`
	Name  string  `json:"name"`
	X     float64 `json:"x"`
	Y     float64 `json:"y"`
	Hp    float64 `json:"hp"`
	HpMax float64 `json:"hpMax"`
}
