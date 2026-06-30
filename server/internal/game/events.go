package game

// HealEvent is emitted when a heal-over-time tick completes.
type HealEvent struct {
	PlayerID int64
	Amount   int
	Ability  string
	Hp       float64
	HpMax    float64
}
