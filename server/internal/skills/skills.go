package skills

import (
	"math"
	"strings"
)

const MaxLevel = 99

// All trainable skill ids (RuneScape-style).
const (
	Attack      = "attack"
	Strength    = "strength"
	Defense     = "defense"
	Hitpoints   = "hitpoints"
	Fishing     = "fishing"
	Woodcutting = "woodcutting"
	Mining      = "mining"
	Farming     = "farming"
	Cooking     = "cooking"
)

// All returns every skill id in display order.
func All() []string {
	return []string{
		Attack, Strength, Defense, Hitpoints,
		Woodcutting, Mining, Fishing, Farming, Cooking,
	}
}

// xpAtLevel returns total XP required to reach level (level 1 = 0).
func xpAtLevel(level int) int64 {
	if level <= 1 {
		return 0
	}
	if level > MaxLevel {
		level = MaxLevel
	}
	var points float64
	for i := 1; i < level; i++ {
		points += math.Floor(float64(i) + 300*math.Pow(2, float64(i)/7))
	}
	return int64(math.Floor(points / 4))
}

// LevelForXP returns the current level for a total XP amount.
func LevelForXP(xp int64) int {
	if xp <= 0 {
		return 1
	}
	for level := MaxLevel; level >= 1; level-- {
		if xp >= xpAtLevel(level) {
			return level
		}
	}
	return 1
}

// XpForLevel returns total XP needed to reach exactly this level.
func XpForLevel(level int) int64 {
	return xpAtLevel(level)
}

// XpToNextLevel returns XP remaining until the next level (0 if maxed).
func XpToNextLevel(xp int64) int64 {
	level := LevelForXP(xp)
	if level >= MaxLevel {
		return 0
	}
	return xpAtLevel(level+1) - xp
}

// ProgressToNext returns 0..1 progress within the current level.
func ProgressToNext(xp int64) float64 {
	level := LevelForXP(xp)
	if level >= MaxLevel {
		return 1
	}
	cur := xpAtLevel(level)
	next := xpAtLevel(level + 1)
	if next <= cur {
		return 1
	}
	return float64(xp-cur) / float64(next-cur)
}

// HitpointsMax returns max HP from hitpoints skill level (100 at level 1, +10 per level).
func HitpointsMax(hpLevel int) float64 {
	if hpLevel < 1 {
		hpLevel = 1
	}
	return float64(90 + hpLevel*10)
}

// Set holds per-skill XP for one character.
type Set map[string]int64

func NewSet() Set {
	s := make(Set, len(All()))
	for _, id := range All() {
		s[id] = 0
	}
	return s
}

func (s Set) Level(skill string) int {
	return LevelForXP(s[skill])
}

func (s Set) TotalXP() int64 {
	var total int64
	for _, xp := range s {
		total += xp
	}
	return total
}

func (s Set) TotalLevel() int {
	var total int
	for _, id := range All() {
		total += s.Level(id)
	}
	return total
}

// Grant adds XP to a skill and returns new xp, level, and whether a level-up occurred.
func (s Set) Grant(skill string, amount int64) (newXp int64, level int, leveledUp bool) {
	if amount <= 0 {
		return s[skill], s.Level(skill), false
	}
	oldLevel := s.Level(skill)
	s[skill] += amount
	newXp = s[skill]
	level = s.Level(skill)
	return newXp, level, level > oldLevel
}

// InteractSkill maps interactable target ids to skill training.
func InteractSkill(targetID string) (skill string, xp int64, ok bool) {
	switch {
	case containsAny(targetID, "tree", "oak", "pine"):
		return Woodcutting, 38, true
	case containsAny(targetID, "rock", "iron", "copper", "mine"):
		return Mining, 35, true
	case containsAny(targetID, "fish"):
		return Fishing, 30, true
	case containsAny(targetID, "farm", "crop", "plot"):
		return Farming, 28, true
	case containsAny(targetID, "stove", "cooking", "fire_pit", "hearth"):
		return Cooking, 32, true
	default:
		return "", 0, false
	}
}

func containsAny(s string, parts ...string) bool {
	for _, p := range parts {
		if p != "" && strings.Contains(s, p) {
			return true
		}
	}
	return false
}

// CombatXP returns skill XP grants for a successful hit.
func CombatXP(ability string, damage int) (attackerSkills map[string]int64, defenderSkills map[string]int64) {
	if damage <= 0 {
		return nil, nil
	}
	d := int64(damage)
	attackerSkills = map[string]int64{
		Attack:    d * 4,
		Strength:  d * 4,
		Hitpoints: int64(math.Max(1, float64(d)/3)),
	}
	defenderSkills = map[string]int64{
		Defense:   d * 4,
		Hitpoints: int64(math.Max(1, float64(d)/3)),
	}
	// Ranged/magic abilities could split differently later.
	if ability == "fireball" || ability == "ice_shard" || ability == "poison_bolt" || ability == "arc_bolt" || ability == "blood_bolt" {
		delete(attackerSkills, Strength)
	}
	return attackerSkills, defenderSkills
}
