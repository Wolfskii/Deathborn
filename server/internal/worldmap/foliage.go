package worldmap

import "math"

const (
	foliageSeed        = 0xB00B5
	foliageLandStride  = 2
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
	// trunkSortY is the tree trunk-base line; entity feet north of this pass through canopy.
	// Zero for rocks.
	trunkSortY float64
	// Square trunk collider (trees): half-width, height, bottom Y at sprite anchor.
	squareHalf   float64
	squareHeight float64
	squareBottom float64
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

			if treeRoll < 16 && m.isInland(tx, ty) {
				idx.add(foliageTree, posX, posY, tx, ty)
			}
		}
	}

	return idx
}

const treeStemColliderRows = 13.0

const treeStemColliderBottomInset = 5.0

// Stem may extend this far north of trunkSortY; deeper into the canopy is pass-through only.
const treeStemSortPad = 2.0

func (idx *foliageIndex) add(kind foliageKind, x, y float64, tx, ty int) {
	scale := 0.78 + float64(foliageHash(tx, ty, 10)%1000)/1000.0*0.38
	scale *= foliageScaleMul(kind)
	variant := foliageVariant(kind, tx, ty)

	var centerY, radius, trunkSortY float64
	var squareHalf, squareHeight, squareBottom float64
	switch kind {
	case foliageTree:
		var footInset float64
		footInset, _, squareHalf = treeColliderMetrics(variant, scale)
		trunkSortY = y - footInset*scale
		squareBottom = y - treeStemColliderBottomInset*scale
		top := y - treeStemColliderRows*scale
		solidTop := trunkSortY - treeStemSortPad*scale
		if top < solidTop {
			top = solidTop
		}
		squareHeight = squareBottom - top
		if squareHeight < 0.5*scale {
			squareHeight = 0.5 * scale
		}
		// Shorten from the bottom only — top stays put (40% less height).
		squareHeight *= 0.6
		squareBottom = top + squareHeight
	default:
		var footInset float64
		footInset, radius = foliageCollider(kind, variant, scale)
		centerY = y - footInset*scale - radius
	}

	idx.circles = append(idx.circles, foliageCircle{
		x:            x,
		y:            centerY,
		radius:       radius,
		trunkSortY:   trunkSortY,
		squareHalf:   squareHalf,
		squareHeight: squareHeight,
		squareBottom: squareBottom,
	})
}

func foliageScaleMul(kind foliageKind) float64 {
	switch kind {
	case foliageTree:
		return 2.75
	case foliageRock:
		return 1.15
	default:
		return 1
	}
}

func treeColliderMetrics(variant int, scale float64) (footInset, _ float64, radius float64) {
	if variant == 0 {
		return 6, 0, 5 * scale
	}
	return 8, 0, 6 * scale
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

func ellipseContainsPoint(cx, cy, rx, ry, px, py float64) bool {
	dx := (px - cx) / rx
	dy := (py - cy) / ry
	return dx*dx+dy*dy <= 1
}

func ellipseOverlapsCircle(ex, ey, rx, ry, ox, oy, or float64) bool {
	if ellipseContainsPoint(ex, ey, rx, ry, ox, oy) {
		return true
	}
	dx := ox - ex
	dy := oy - ey
	distNorm := math.Sqrt((dx/rx)*(dx/rx) + (dy/ry)*(dy/ry))
	if distNorm < 0.0001 {
		return or >= math.Min(rx, ry)
	}
	closestX := ex + dx/distNorm*rx
	closestY := ey + dy/distNorm*ry
	cdx := ox - closestX
	cdy := oy - closestY
	return cdx*cdx+cdy*cdy < or*or
}

func ellipseOverlapsRect(ex, ey, rx, ry, left, right, top, bottom float64) bool {
	closestX := ex
	if closestX < left {
		closestX = left
	} else if closestX > right {
		closestX = right
	}
	closestY := ey
	if closestY < top {
		closestY = top
	} else if closestY > bottom {
		closestY = bottom
	}
	if ellipseContainsPoint(ex, ey, rx, ry, closestX, closestY) {
		return true
	}
	corners := [4][2]float64{{left, top}, {right, top}, {left, bottom}, {right, bottom}}
	for i := range corners {
		if ellipseContainsPoint(ex, ey, rx, ry, corners[i][0], corners[i][1]) {
			return true
		}
	}
	return false
}

func pushEllipseOutOfRect(ex, ey, rx, ry, left, right, top, bottom float64) (float64, float64, bool) {
	closestX := ex
	if closestX < left {
		closestX = left
	} else if closestX > right {
		closestX = right
	}
	closestY := ey
	if closestY < top {
		closestY = top
	} else if closestY > bottom {
		closestY = bottom
	}
	dx := ex - closestX
	dy := ey - closestY
	if !ellipseContainsPoint(ex, ey, rx, ry, closestX, closestY) && dx*dx+dy*dy >= 0.0001 {
		return ex, ey, false
	}
	if dx*dx+dy*dy < 0.0001 {
		dx = 0
		dy = 1
	}
	dist := math.Sqrt(dx*dx + dy*dy)
	nx := dx / dist
	ny := dy / dist
	effR := 1 / math.Sqrt((nx/rx)*(nx/rx)+(ny/ry)*(ny/ry))
	push := effR - dist + 0.35
	return ex + nx*push, ey + ny*push, true
}

func (idx *foliageIndex) resolveMoveBlock(fromX, fromY, toX, toY, entityRx, entityRy float64) (float64, float64) {
	if idx == nil {
		return toX, toY
	}

	// Depenetrate if already inside a trunk (walked down from behind into the stem).
	if idx.feetWouldCollide(fromX, fromY, entityRx, entityRy) {
		newX, newY := idx.resolvePosition(fromX, fromY, entityRx, entityRy)
		toX += newX - fromX
		toY += newY - fromY
		fromX, fromY = newX, newY
	}

	if !idx.feetWouldCollide(toX, toY, entityRx, entityRy) {
		return toX, toY
	}
	if !idx.feetWouldCollide(toX, fromY, entityRx, entityRy) {
		return toX, fromY
	}
	if !idx.feetWouldCollide(fromX, toY, entityRx, entityRy) {
		return fromX, toY
	}

	dx := toX - fromX
	dy := toY - fromY
	len := math.Sqrt(dx*dx + dy*dy)
	if len <= 0.001 {
		return fromX, fromY
	}
	dirX := dx / len
	dirY := dy / len
	bestX, bestY := fromX, fromY
	lo, hi := 0.0, 1.0
	for i := 0; i < 7; i++ {
		mid := (lo + hi) * 0.5
		tryX := fromX + dirX*len*mid
		tryY := fromY + dirY*len*mid
		if !idx.feetWouldCollide(tryX, tryY, entityRx, entityRy) {
			bestX, bestY = tryX, tryY
			lo = mid
		} else {
			hi = mid
		}
	}
	if lo > 0.001 {
		return bestX, bestY
	}
	return fromX, fromY
}

func (idx *foliageIndex) feetWouldCollide(x, y, entityRx, entityRy float64) bool {
	if idx == nil {
		return false
	}
	cy := playerCollisionY(y)
	for i := range idx.circles {
		f := &idx.circles[i]
		if f.trunkSortY > 0 && y < f.trunkSortY {
			continue
		}
		if f.squareHalf > 0 {
			top := f.squareBottom - f.squareHeight
			if ellipseOverlapsRect(x, cy, entityRx, entityRy, f.x-f.squareHalf, f.x+f.squareHalf, top, f.squareBottom) {
				return true
			}
			continue
		}
		if ellipseOverlapsCircle(x, cy, entityRx, entityRy, f.x, f.y, f.radius) {
			return true
		}
	}
	return false
}

func (idx *foliageIndex) resolvePosition(x, y, entityRx, entityRy float64) (float64, float64) {
	if idx == nil {
		return x, y
	}
	cy := playerCollisionY(y)
	for iter := 0; iter < 4; iter++ {
		pushed := false
		for i := range idx.circles {
			f := &idx.circles[i]
			if f.trunkSortY > 0 && y < f.trunkSortY {
				continue
			}
			if f.squareHalf > 0 {
				top := f.squareBottom - f.squareHeight
				var ok bool
				x, cy, ok = pushEllipseOutOfRect(x, cy, entityRx, entityRy, f.x-f.squareHalf, f.x+f.squareHalf, top, f.squareBottom)
				if ok {
					pushed = true
				}
				continue
			}
			if !ellipseOverlapsCircle(x, cy, entityRx, entityRy, f.x, f.y, f.radius) {
				continue
			}
			dx := x - f.x
			dy := cy - f.y
			minDist := f.radius + math.Max(entityRx, entityRy)
			distSq := dx*dx + dy*dy
			if distSq < 0.0001 {
				continue
			}
			dist := math.Sqrt(distSq)
			push := (minDist - dist) / dist
			x += dx * push
			cy += dy * push
			pushed = true
		}
		if !pushed {
			break
		}
	}
	return x, feetFromCollisionY(cy)
}

type townRect struct {
	cx, cy, halfW, halfH float64
}

func (m *Map) townExclusions() []townRect {
	scale := m.TileSize / 16.0
	ts := m.TileSize
	tileCenter := func(tx, ty int) (float64, float64) {
		return (float64(tx) + 0.5) * ts, (float64(ty) + 0.5) * ts
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
