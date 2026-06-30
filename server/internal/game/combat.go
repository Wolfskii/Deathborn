package game

const (
	DefaultHpMax   = 100
	SlashDamage    = 5
	FireballDamage = 25
	IceShardDamage = 18
	ArcBoltDamage     = 14
	BloodBoltDamage   = 22
	PoisonCloudDamage = 6

	ShieldBashDamage   = 12
	WhirlwindDamage    = 8
	WarriorDashDamage  = 10
	SecondWindHeal     = 15

	ShieldBashRange    = 56
	WhirlwindRadius    = 52
	WarriorDashRange   = 110
	HunterMarkRange    = 200
	BandageTotalHeal   = 25
	BandageHoTTicks    = 5
	BandageHoTInterval = 1.0 // seconds between ticks
)

// DamageForAbility returns server-authoritative damage for a damaging ability id.
func DamageForAbility(ability string) int {
	switch ability {
	case "slash":
		return SlashDamage
	case "fireball":
		return FireballDamage
	case "ice_shard":
		return IceShardDamage
	case "arc_bolt":
		return ArcBoltDamage
	case "blood_bolt":
		return BloodBoltDamage
	case "poison_cloud":
		return PoisonCloudDamage
	case "shield_bash":
		return ShieldBashDamage
	case "whirlwind":
		return WhirlwindDamage
	case "warrior_dash":
		return WarriorDashDamage
	default:
		return 0
	}
}

// MaxHitRange returns the maximum distance allowed between attacker and target.
func MaxHitRange(ability string) float64 {
	switch ability {
	case "slash":
		return 72
	case "fireball", "ice_shard":
		return 560
	case "arc_bolt", "blood_bolt":
		return 140
	case "poison_cloud":
		return 90
	case "shield_bash":
		return ShieldBashRange
	case "whirlwind":
		return WhirlwindRadius
	case "warrior_dash":
		return WarriorDashRange
	default:
		return 0
	}
}

// HealForAbility returns instant healing for a self-target ability/item id.
func HealForAbility(ability string) int {
	switch ability {
	case "second_wind":
		return SecondWindHeal
	default:
		return 0
	}
}

// BandageHoTPerTick returns healing per bandage tick.
func BandageHoTPerTick() int {
	if BandageHoTTicks <= 0 {
		return BandageTotalHeal
	}
	return BandageTotalHeal / BandageHoTTicks
}
