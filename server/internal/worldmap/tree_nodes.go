package worldmap

// TreePoint is a tree's trunk-base position in world pixels.
type TreePoint struct{ X, Y float64 }

// TreePositions returns the trunk base of every tree the map generates, in a
// stable order. Woodcutting nodes are built from these so that gathering lines
// up with the trees the map places.
func (m *Map) TreePositions() []TreePoint {
	if m == nil || m.foliage == nil {
		return nil
	}
	var out []TreePoint
	for _, c := range m.foliage.circles {
		if c.kind == foliageTree {
			out = append(out, TreePoint{X: c.x, Y: c.trunkSortY})
		}
	}
	return out
}
