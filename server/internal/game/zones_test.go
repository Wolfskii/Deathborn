package game

import (
	"bytes"
	"os"
	"path/filepath"
	"runtime"
	"testing"

	"github.com/deathborn/server/internal/worldmap"
)

func testTerrain(tileSize float64) *worldmap.Map {
	return &worldmap.Map{TileSize: tileSize, DefaultSpawnX: 1000, DefaultSpawnY: 1000}
}

func TestZonesEmbedMatchesShared(t *testing.T) {
	_, thisFile, _, ok := runtime.Caller(0)
	if !ok {
		t.Fatal("runtime.Caller failed")
	}
	repoRoot := filepath.Clean(filepath.Join(filepath.Dir(thisFile), "..", "..", ".."))
	shared := filepath.Join(repoRoot, "shared", "zones.json")
	embedded := filepath.Join(repoRoot, "server", "internal", "game", "zones.json")

	want, err := os.ReadFile(shared)
	if err != nil {
		t.Fatalf("read shared: %v", err)
	}
	got, err := os.ReadFile(embedded)
	if err != nil {
		t.Fatalf("read embed copy: %v", err)
	}
	if !bytes.Equal(want, got) {
		t.Fatal("zones.json embed is stale - run: py -3 scripts/sync_shared_embeds.py")
	}
}

// The towns must match the sizes that were hard-coded before zones.json existed
// (pixel half sizes at the 16px reference tile, scaled by tile size).
func TestTownSizesMatchOriginalLayout(t *testing.T) {
	for _, ts := range []float64{16, 32} {
		idx := NewZoneIndex(testTerrain(ts))
		scale := ts / 16.0
		want := map[string][2]float64{
			"starter_town": {148 * scale, 128 * scale},
			"northhaven":   {136 * scale, 118 * scale},
			"westmere":     {128 * scale, 112 * scale},
			"eastwatch":    {132 * scale, 116 * scale},
			"southport":    {140 * scale, 120 * scale},
		}
		if len(idx.zones) != len(want) {
			t.Fatalf("tile %v: %d zones loaded, want %d", ts, len(idx.zones), len(want))
		}
		for _, zd := range idx.zones {
			w, ok := want[zd.id]
			if !ok {
				t.Fatalf("unexpected zone %q", zd.id)
			}
			if zd.halfW != w[0] || zd.halfH != w[1] {
				t.Fatalf("%s at tile %v: half size (%v,%v), want (%v,%v)", zd.id, ts, zd.halfW, zd.halfH, w[0], w[1])
			}
		}
	}
}

func TestSpawnTownIsSafeAndBlocksPvP(t *testing.T) {
	w := &World{zones: NewZoneIndex(testTerrain(16))}
	if !w.zones.InSafeArea(1000, 1000) {
		t.Fatal("spawn point should be a safe area")
	}
	if w.zones.PvPAllowed(1000, 1000) {
		t.Fatal("PvP must be off inside a safe town")
	}
	if !w.zones.PvPAllowed(5000, 5000) {
		t.Fatal("PvP should be on outside every town")
	}
}

func TestZoneListAndZoneAt(t *testing.T) {
	w := &World{zones: NewZoneIndex(testTerrain(16))}
	list := w.ZoneList()
	if len(list) != 5 || list[0].Name != "Starter Town" {
		t.Fatalf("zone list = %+v, want 5 zones starting with Starter Town", list)
	}
	z, ok := w.ZoneAt(1000, 1000)
	if !ok || z.ID != "starter_town" || !z.Safe {
		t.Fatalf("ZoneAt(spawn) = %+v, %v", z, ok)
	}
	if _, ok := w.ZoneAt(5000, 5000); ok {
		t.Fatal("ZoneAt should report no zone in the wilderness")
	}
}

func TestNilTerrainHasNoZones(t *testing.T) {
	idx := NewZoneIndex(nil)
	if len(idx.zones) != 0 || !idx.PvPAllowed(0, 0) {
		t.Fatal("nil terrain should yield an empty index where everything is PvP")
	}
}
