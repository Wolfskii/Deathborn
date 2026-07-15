package abilities_test

import (
	"testing"

	"github.com/deathborn/server/internal/game/abilities"
)

func TestEmbeddedCatalog(t *testing.T) {
	c := abilities.Default()
	if c.Damage("whirlwind") != 8 {
		t.Fatalf("whirlwind damage = %d, want 8", c.Damage("whirlwind"))
	}
	if c.HitRange("whirlwind") != 76 {
		t.Fatalf("whirlwind range = %v, want 76", c.HitRange("whirlwind"))
	}
	if c.Heal("second_wind") != 15 {
		t.Fatalf("second_wind heal = %d, want 15", c.Heal("second_wind"))
	}
	hot, ok := c.HoT("bandage")
	if !ok || hot.Ticks != 5 || hot.TotalHeal != 25 {
		t.Fatalf("bandage hot = %+v, ok=%v", hot, ok)
	}
	def, ok := c.Get("warrior_dash")
	if !ok || def.Kind != abilities.KindMelee {
		t.Fatalf("warrior_dash kind = %q", def.Kind)
	}
}
