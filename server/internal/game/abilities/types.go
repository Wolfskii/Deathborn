package abilities

// Kind groups abilities for validation and future behaviour hooks.
type Kind string

const (
	KindMelee   Kind = "melee"
	KindSpell   Kind = "spell"
	KindHeal    Kind = "heal"
	KindBuff    Kind = "buff"
	KindUtility Kind = "utility"
	KindItem    Kind = "item"
)

// CombatSpec holds server-authoritative hit / damage numbers.
type CombatSpec struct {
	Damage   int     `json:"damage,omitempty"`
	HitRange float64 `json:"hitRange,omitempty"`
	Heal     int     `json:"heal,omitempty"`
}

// HoTSpec describes heal-over-time item abilities (e.g. bandage).
type HoTSpec struct {
	TotalHeal   int     `json:"totalHeal"`
	Ticks       int     `json:"ticks"`
	IntervalSec float64 `json:"intervalSec"`
}

// Def is the base record for every ability. Optional sections depend on kind.
type Def struct {
	ID     string      `json:"id"`
	Kind   Kind        `json:"kind"`
	Combat *CombatSpec `json:"combat,omitempty"`
	HoT    *HoTSpec    `json:"hot,omitempty"`
}

func (d Def) Damage() int {
	if d.Combat == nil {
		return 0
	}
	return d.Combat.Damage
}

func (d Def) HitRange() float64 {
	if d.Combat == nil {
		return 0
	}
	return d.Combat.HitRange
}

func (d Def) Heal() int {
	if d.Combat == nil {
		return 0
	}
	return d.Combat.Heal
}

func (d Def) HoTPerTick() int {
	if d.HoT == nil || d.HoT.Ticks <= 0 {
		return 0
	}
	return d.HoT.TotalHeal / d.HoT.Ticks
}

type file struct {
	Version   int    `json:"version"`
	Abilities []Def  `json:"abilities"`
}
