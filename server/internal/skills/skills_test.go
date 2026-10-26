package skills

import "testing"

func TestHighestSkillLevelEmptySet(t *testing.T) {
	if got := NewSet().HighestSkillLevel(); got != 1 {
		t.Fatalf("empty set highest level = %d, want 1", got)
	}
}

func TestHighestSkillLevelPicksBestSkill(t *testing.T) {
	s := NewSet()
	s[Hitpoints] = XpForLevel(10)
	s[Mining] = XpForLevel(25)
	s[Attack] = XpForLevel(5)

	if got := s.HighestSkillLevel(); got != 25 {
		t.Fatalf("highest level = %d, want 25", got)
	}
}
