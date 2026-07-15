package game

import "github.com/deathborn/server/internal/game/abilities"

const DefaultHpMax = 100

// Ability catalog accessors — data lives in abilities.json (see shared/abilities.json mirror).

func DamageForAbility(id string) int {
	return abilities.Default().Damage(id)
}

func MaxHitRange(id string) float64 {
	return abilities.Default().HitRange(id)
}

func HealForAbility(id string) int {
	return abilities.Default().Heal(id)
}

func BandageHoTPerTick() int {
	if hot, ok := abilities.Default().HoT("bandage"); ok {
		return hot.TotalHeal / hot.Ticks
	}
	return 0
}

func BandageHoTTicks() int {
	if hot, ok := abilities.Default().HoT("bandage"); ok {
		return hot.Ticks
	}
	return 0
}

func BandageHoTInterval() float64 {
	if hot, ok := abilities.Default().HoT("bandage"); ok {
		return hot.IntervalSec
	}
	return 0
}

// Named ranges used outside hit validation (dash distance, hunter mark cone, etc.).
func AbilityHitRange(id string) float64 {
	return abilities.Default().HitRange(id)
}
