package abilities_test

import (
	"bytes"
	"os"
	"path/filepath"
	"runtime"
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
	if c.HitRange("warrior_dash") != 200 {
		t.Fatalf("warrior_dash hitRange = %v, want 200", c.HitRange("warrior_dash"))
	}
}

// Fail if server embed drifted from shared/abilities.json (run: task sync:shared).
func TestAbilitiesEmbedMatchesShared(t *testing.T) {
	_, thisFile, _, ok := runtime.Caller(0)
	if !ok {
		t.Fatal("runtime.Caller failed")
	}
	repoRoot := filepath.Clean(filepath.Join(filepath.Dir(thisFile), "..", "..", "..", ".."))
	shared := filepath.Join(repoRoot, "shared", "abilities.json")
	embedded := filepath.Join(repoRoot, "server", "internal", "game", "abilities", "abilities.json")

	want, err := os.ReadFile(shared)
	if err != nil {
		t.Fatalf("read shared: %v", err)
	}
	got, err := os.ReadFile(embedded)
	if err != nil {
		t.Fatalf("read embed copy: %v", err)
	}
	if !bytes.Equal(want, got) {
		t.Fatalf("abilities.json embed is stale — run: python scripts/sync_shared_embeds.py\n  shared=%s\n  embed=%s", shared, embedded)
	}
}
