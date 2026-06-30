package game

const (
	DefaultHpMax    = 100
	SlashDamage     = 5
	FireballDamage  = 25
	HealAmount      = 25
	BandageAmount   = 15
)

// DamageForAbility returns server-authoritative damage for a damaging ability id.
func DamageForAbility(ability string) int {
	switch ability {
	case "slash":
		return SlashDamage
	case "fireball":
		return FireballDamage
	default:
		return 0
	}
}

// HealForAbility returns server-authoritative healing for a self-target ability/item id.
func HealForAbility(ability string) int {
	switch ability {
	case "heal":
		return HealAmount
	case "bandage":
		return BandageAmount
	default:
		return 0
	}
}
