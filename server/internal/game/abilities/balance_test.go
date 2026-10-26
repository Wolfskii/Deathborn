package abilities

import "testing"

// TestAbilityDataInvariants guards shared/abilities.json against values the
// server cannot use: duplicate ids, negative numbers, damage with no reach, and
// heal-over-time specs that would never tick.
func TestAbilityDataInvariants(t *testing.T) {
	cat := Default()
	seen := map[string]bool{}
	for id, def := range cat.byID {
		if seen[id] {
			t.Fatalf("duplicate ability id %q", id)
		}
		seen[id] = true

		if def.Combat != nil {
			if def.Combat.Damage < 0 || def.Combat.HitRange < 0 || def.Combat.Heal < 0 {
				t.Errorf("%s: combat numbers must not be negative: %+v", id, def.Combat)
			}
			if def.Combat.Damage > 0 && def.Combat.HitRange <= 0 {
				t.Errorf("%s: deals damage but has no hitRange", id)
			}
		}
		if def.HoT != nil {
			if def.HoT.TotalHeal < 0 || def.HoT.Ticks <= 0 || def.HoT.IntervalSec <= 0 {
				t.Errorf("%s: heal over time would never tick: %+v", id, def.HoT)
			}
		}
	}
}
