package worldmap

import "math"

const (
	foliageSeed        = 0xB00B5
	foliageLandStride  = 2
	foliageWaterStride = 3
	foliageTownPad     = 56.0
	foliageSpawnClear  = 160.0
)

type foliageKind int

const (
	foliageBush foliageKind = iota
	foliageTree
	foliageRock
	foliageWaterRock
)

type foliageCircle struct {
	x, y, radius float64
}

type foliageIndex struct {
	circles []foliageCircle
}

func (m *Map) buildFoliage() *foliageIndex {
	idx := &foliageIndex{}
	towns := m.townExclusions()
	scale := m.TileSize / 16.0
	townPad := foliageTownPad * scale
	spawnClear := foliageSpawnClear * scale

	for ty := 0; ty < m.TileHeight; ty += foliageLandStride {
		for tx := 0; tx < m.TileWidth; tx += foliageLandStride {
			if !m.isLand(tx, ty) {
				continue
			}
			posX, posY := m.jitteredPosition(tx, ty)
			if m.inTown(towns, townPad, posX, posY) || m.nearSpawn(spawnClear, posX, posY) {
				continue
			}

			treeRoll := foliageHash(tx, ty, 1) % 1000
			rockRoll := foliageHash(tx, ty, 2) % 1000
			bushRoll := foliageHash(tx, ty, 3) % 1000

			switch {
			case treeRoll < 16 && m.isInland(tx, ty):
				idx.add(foliageTree, posX, posY, tx, ty)
			case rockRoll < 12:
				idx.add(foliageRock, posX, posY, tx, ty)
			case bushRoll < 28:
				// Bushes are pass-through on the client; no server collider.
			}
		}
	}

	for ty := 0; ty < m.TileHeight; ty += foliageWaterStride {
		for tx := 0; tx < m.TileWidth; tx += foliageWaterStride {
			if m.isLand(tx, ty) {
				continue
			}
			posX, posY := m.jitteredPosition(tx, ty)
			if m.inTown(towns, townPad, posX, posY) {
				continue
			}
			if foliageHash(tx, ty, 4)%1000 >= 22 {
				continue
			}
			idx.add(foliageWaterRock, posX, posY, tx, ty)
		}
	}

	return idx
}

func (idx *foliageIndex) add(kind foliageKind, x, y float64, tx, ty int) {
	scale := 0.78 + float64(foliageHash(tx, ty, 10)%1000)/1000.0*0.38
	variant := foliageVariant(kind, tx, ty)
	footInset, radius := foliageCollider(kind, variant, scale)
	// Lift the circle by its radius so its bottom edge sits at the visual foot
	// (matches client WorldFoliage.ColliderCenter); otherwise the collider
	// extends below the sprite image.
	idx.circles = append(idx.circles, foliageCircle{
		x:      x,
		y:      y - footInset*scale - radius,
		radius: radius,
	})
}

func foliageVariant(kind foliageKind, tx, ty int) int {
	roll := foliageHash(tx, ty, 12)
	switch kind {
	case foliageBush:
		return int(roll % 3)
	case foliageTree:
		return int(roll % 2)
	case foliageRock, foliageWaterRock:
		return int(roll % 4)
	default:
		return 0
	}
}

func foliageCollider(kind foliageKind, variant int, scale float64) (footInset, radius float64) {
	switch kind {
	case foliageTree:
		if variant == 0 {
			footInset = 23
		} else {
			footInset = 25
		}
		return footInset, 8 * scale
	case foliageRock:
		switch variant {
		case 0:
			return 14, 15 * scale
		case 1:
			return 12, 16 * scale
		case 2:
			return 13, 14 * scale
		default:
			return 9, 15 * scale
		}
	case foliageWaterRock:
		return 17, 12 * scale
	default:
		return 0, 0
	}
}

func (idx *foliageIndex) resolvePosition(x, y, entityRadius float64) (float64, float64) {
	if idx == nil {
		return x, y
	}
	for iter := 0; iter < 4; iter++ {
		pushed := false
		for i := range idx.circles {
			f := &idx.circles[i]
			dx := x - f.x
			dy := y - f.y
			minDist := f.radius + entityRadius
			distSq := dx*dx + dy*dy
			if distSq >= minDist*minDist || distSq < 0.0001 {
				continue
			}
			dist := math.Sqrt(distSq)
			push := (minDist - dist) / dist
			x += dx * push
			y += dy * push
			pushed = true
		}
		if !pushed {
			break
		}
	}
	return x, y
}

type townRect struct {
	cx, cy, halfW, halfH float64
}

func (m *Map) townExclusions() []townRect {
	scale := m.TileSize / 16.0
	ts := m.TileSize
	tileCenter := func(tx, ty int) (float64, float64) {
		return (float64(tx)+0.5)*ts, (float64(ty)+0.5)*ts
	}
	nx, ny := tileCenter(234, 45)
	wx, wy := tileCenter(39, 189)
	ex, ey := tileCenter(229, 128)
	sx, sy := tileCenter(112, 281)
	return []townRect{
		{m.DefaultSpawnX, m.DefaultSpawnY, 148 * scale, 128 * scale},
		{nx, ny, 136 * scale, 118 * scale},
		{wx, wy, 128 * scale, 112 * scale},
		{ex, ey, 132 * scale, 116 * scale},
		{sx, sy, 140 * scale, 120 * scale},
	}
}

func (m *Map) inTown(towns []townRect, pad, x, y float64) bool {
	for i := range towns {
		t := &towns[i]
		if x >= t.cx-t.halfW-pad && x <= t.cx+t.halfW+pad &&
			y >= t.cy-t.halfH-pad && y <= t.cy+t.halfH+pad {
			return true
		}
	}
	return false
}

func (m *Map) nearSpawn(clearRadius, x, y float64) bool {
	dx := x - m.DefaultSpawnX
	dy := y - m.DefaultSpawnY
	return dx*dx+dy*dy < clearRadius*clearRadius
}

func (m *Map) jitteredPosition(tx, ty int) (float64, float64) {
	ts := m.TileSize
	jx := float64(foliageHash(tx, ty, 20)%1000)/1000.0*ts*0.7 - ts*0.35
	jy := float64(foliageHash(tx, ty, 21)%1000)/1000.0*ts*0.7 - ts*0.35
	return (float64(tx)+0.5)*ts + jx, (float64(ty)+0.5)*ts + jy
}

func (m *Map) isInland(tx, ty int) bool {
	return m.isLand(tx, ty-1) && m.isLand(tx+1, ty) &&
		m.isLand(tx, ty+1) && m.isLand(tx-1, ty)
}

func foliageHash(tx, ty, salt int) uint32 {
	h := uint32(foliageSeed) ^ uint32(tx*73856093) ^ uint32(ty*19349663) ^ uint32(salt*83492791)
	h ^= h >> 16
	h *= 0x85ebca6b
	h ^= h >> 13
	h *= 0xc2b2ae35
	h ^= h >> 16
	return h
}
