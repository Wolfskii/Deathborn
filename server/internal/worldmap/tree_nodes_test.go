package worldmap

import "testing"

func TestTreePositionsOnEmbeddedMap(t *testing.T) {
	m, err := LoadEmbedded()
	if err != nil {
		t.Fatalf("load embedded map: %v", err)
	}
	trees := m.TreePositions()
	if len(trees) == 0 {
		t.Fatal("embedded map should place at least one tree")
	}
	maxX := float64(m.TileWidth) * m.TileSize
	maxY := float64(m.TileHeight) * m.TileSize
	for _, tp := range trees {
		if tp.X < 0 || tp.X > maxX || tp.Y < 0 || tp.Y > maxY {
			t.Fatalf("tree at (%v,%v) is outside the %vx%v map", tp.X, tp.Y, maxX, maxY)
		}
	}

	again := m.TreePositions()
	if len(again) != len(trees) || again[0] != trees[0] || again[len(again)-1] != trees[len(trees)-1] {
		t.Fatal("tree order must be stable so node ids do not change between starts")
	}
}

func TestTreePositionsNilMap(t *testing.T) {
	var m *Map
	if got := m.TreePositions(); got != nil {
		t.Fatalf("nil map should have no trees, got %d", len(got))
	}
}
