package worldmap

import "math"

// Grass tuft placement — mirrors client FarmRpgGrassProps (no collider, blocks homestead plots).

const (
	grassPropsSeed            = 0x06A5500
	grassSpawnRollMax         = 1000
	grassSoloSpawnChance      = 55
	grassPatchSpawnChanceMin  = 340
	grassPatchSpawnChanceMax  = 820
	grassPatchMacroSize       = 8
	grassPatchMacroChance     = 300
	grassPropsTownPad         = 56.0
	grassPropsSpawnClear      = 160.0
)

// PlotHasGrassTufts reports whether procedural grass overlays would appear inside the plot.
func (m *Map) PlotHasGrassTufts(cx, cy, halfW, halfH float64) bool {
	if m.elevation == nil {
		return false
	}
	ts := m.TileSize
	tx0 := int(math.Floor((cx - halfW) / ts))
	ty0 := int(math.Floor((cy - halfH) / ts))
	tx1 := int(math.Floor((cx + halfW) / ts))
	ty1 := int(math.Floor((cy + halfH) / ts))
	if tx0 < 0 {
		tx0 = 0
	}
	if ty0 < 0 {
		ty0 = 0
	}
	if tx1 >= m.TileWidth {
		tx1 = m.TileWidth - 1
	}
	if ty1 >= m.TileHeight {
		ty1 = m.TileHeight - 1
	}
	for ty := ty0; ty <= ty1; ty++ {
		for tx := tx0; tx <= tx1; tx++ {
			if !grassShouldPlace(m, tx, ty) {
				continue
			}
			if !grassShouldSpawn(tx, ty) {
				continue
			}
			if grassCatalogLen(m, tx, ty) > 0 {
				return true
			}
		}
	}
	return false
}

func grassCatalogLen(m *Map, tx, ty int) int {
	elev := int(m.elevation.at(tx, ty, m.TileWidth, m.TileHeight))
	if elev < 0 || elev > 4 {
		return 0
	}
	// Bright grass props on elev 0–2 only (matches client).
	if elev <= 2 {
		return 3
	}
	return 0
}

func grassShouldPlace(m *Map, tx, ty int) bool {
	elev := int(m.elevation.at(tx, ty, m.TileWidth, m.TileHeight))
	if elev < 0 || !m.isLand(tx, ty) {
		return false
	}
	if grassIsRampTop(m, tx, ty) || grassIsRampLanding(m, tx, ty) {
		return false
	}
	north := elev
	if ty > 0 {
		north = int(m.elevation.at(tx, ty-1, m.TileWidth, m.TileHeight))
	}
	if north > elev {
		return false
	}
	south := elev
	if ty+1 < m.TileHeight {
		south = int(m.elevation.at(tx, ty+1, m.TileWidth, m.TileHeight))
	}
	if elev > 0 && south < elev && south >= 0 {
		return false
	}
	ts := m.TileSize
	worldX := (float64(tx) + 0.5) * ts
	worldY := (float64(ty) + 0.5) * ts
	if grassNearSpawn(m, worldX, worldY) || grassInTown(m, worldX, worldY) {
		return false
	}
	return true
}

func grassIsRampLanding(m *Map, tx, ty int) bool {
	return m.elevation.rampAt(tx, ty, m.TileWidth, m.TileHeight) != 0
}

func grassIsRampTop(m *Map, tx, ty int) bool {
	if ty+1 >= m.TileHeight {
		return false
	}
	return m.elevation.rampAt(tx, ty+1, m.TileWidth, m.TileHeight) != 0
}

func grassShouldSpawn(tx, ty int) bool {
	patch := grassPatchInfluence(tx, ty)
	threshold := grassSoloSpawnChance
	if patch > 0 {
		threshold = grassPatchSpawnChanceMin + int(patch*float64(grassPatchSpawnChanceMax-grassPatchSpawnChanceMin))
	}
	return grassHash(tx, ty, 1)%grassSpawnRollMax < uint32(threshold)
}

func grassPatchInfluence(tx, ty int) float64 {
	mx := floorDiv(tx, grassPatchMacroSize)
	my := floorDiv(ty, grassPatchMacroSize)
	if grassHash(mx, my, 60)%grassSpawnRollMax >= grassPatchMacroChance {
		return 0
	}
	patchCount := 1 + int(grassHash(mx, my, 61)%2)
	best := 0.0
	for i := 0; i < patchCount; i++ {
		cx := mx*grassPatchMacroSize + int(grassHash(mx, my, 62+i*4)%uint32(grassPatchMacroSize))
		cy := my*grassPatchMacroSize + int(grassHash(mx, my, 63+i*4)%uint32(grassPatchMacroSize))
		radius := 2.2 + float64(grassHash(mx, my, 64+i*4)%1000)/1000.0*2.8
		dx := float64(tx) + 0.5 - float64(cx)
		dy := float64(ty) + 0.5 - float64(cy)
		dist := math.Sqrt(dx*dx + dy * dy)
		if dist >= radius {
			continue
		}
		influence := 1 - dist/radius
		if influence > best {
			best = influence
		}
	}
	return best
}

func floorDiv(value, divisor int) int {
	if value >= 0 {
		return value / divisor
	}
	return (value - divisor + 1) / divisor
}

func grassNearSpawn(m *Map, x, y float64) bool {
	scale := m.TileSize / 16.0
	r := grassPropsSpawnClear * scale
	dx := x - m.DefaultSpawnX
	dy := y - m.DefaultSpawnY
	return dx*dx+dy*dy < r*r
}

func grassInTown(m *Map, x, y float64) bool {
	scale := m.TileSize / 16.0
	pad := grassPropsTownPad * scale
	for _, t := range m.townExclusions() {
		if x >= t.cx-t.halfW-pad && x <= t.cx+t.halfW+pad &&
			y >= t.cy-t.halfH-pad && y <= t.cy+t.halfH+pad {
			return true
		}
	}
	return false
}

func grassHash(tx, ty, salt int) uint32 {
	h := uint32(grassPropsSeed) ^ uint32(tx*73856093) ^ uint32(ty*19349663) ^ uint32(salt*83492791)
	h ^= h >> 16
	h *= 0x85ebca6b
	h ^= h >> 13
	h *= 0xc2b2ae35
	h ^= h >> 16
	return h
}
