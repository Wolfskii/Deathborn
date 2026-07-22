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
	// Full sprite AABB for homestead plot overlap (anchor at feet).
	spriteLeft, spriteTop, spriteRight, spriteBottom float64
}

type foliageIndex struct {
	circles    []foliageCircle
	cellSize   float64
	queryPad   float64
	cells      map[[2]int][]int
}

func (m *Map) buildFoliage() *foliageIndex {
	idx := &foliageIndex{
		cellSize: 128,
		queryPad: 48,
		cells:    make(map[[2]int][]int),
	}
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
	var spriteLeft, spriteTop, spriteRight, spriteBottom float64
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
		const frameW, frameH = 32.0, 48.0
		sw := frameW * scale
		sh := frameH * scale
		spriteLeft = x - sw*0.5
		spriteRight = x + sw*0.5
		spriteBottom = y
		spriteTop = y - sh
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
		spriteLeft:   spriteLeft,
		spriteTop:    spriteTop,
		spriteRight:  spriteRight,
		spriteBottom: spriteBottom,
	})
	ext := radius
	if squareHalf > ext {
		ext = squareHalf
	}
	if ext+4 > idx.queryPad {
		idx.queryPad = ext + 4
	}
	cx := int(math.Floor(x / idx.cellSize))
	cy := int(math.Floor(y / idx.cellSize))
	key := [2]int{cx, cy}
	i := len(idx.circles) - 1
	idx.cells[key] = append(idx.cells[key], i)
}

func (idx *foliageIndex) forEachNear(x, y, radius float64, fn func(i int, f *foliageCircle) bool) bool {
	if idx == nil || len(idx.circles) == 0 {
		return false
	}
	minCx := int(math.Floor((x - radius) / idx.cellSize))
	maxCx := int(math.Floor((x + radius) / idx.cellSize))
	minCy := int(math.Floor((y - radius) / idx.cellSize))
	maxCy := int(math.Floor((y + radius) / idx.cellSize))
	for cy := minCy; cy <= maxCy; cy++ {
		for cx := minCx; cx <= maxCx; cx++ {
			list := idx.cells[[2]int{cx, cy}]
			for _, i := range list {
				if fn(i, &idx.circles[i]) {
					return true
				}
			}
		}
	}
	return false
}

// plotOverlaps reports whether a tree trunk or rock collider intersects a homestead plot.
func (idx *foliageIndex) plotOverlaps(cx, cy, halfW, halfH float64) bool {
	if idx == nil || len(idx.circles) == 0 {
		return false
	}
	plotLeft := cx - halfW
	plotRight := cx + halfW
	plotTop := cy - halfH
	plotBottom := cy + halfH
	pad := math.Max(halfW, halfH) + idx.queryPad
	return idx.forEachNear(cx, cy, pad, func(_ int, f *foliageCircle) bool {
		if f.squareHalf > 0 {
			top := f.squareBottom - f.squareHeight
			return rectOverlapsPlot(f.x-f.squareHalf, top, f.x+f.squareHalf, f.squareBottom,
				plotLeft, plotTop, plotRight, plotBottom)
		}
		if f.radius > 0.001 {
			return circleOverlapsPlot(f.x, f.y, f.radius, plotLeft, plotTop, plotRight, plotBottom)
		}
		return false
	})
}

func rectOverlapsPlot(l, t, r, b, plotL, plotT, plotR, plotB float64) bool {
	return r >= plotL && l <= plotR && b >= plotT && t <= plotB
}

func circleOverlapsPlot(cx, cy, radius, plotL, plotT, plotR, plotB float64) bool {
	closestX := cx
	if closestX < plotL {
		closestX = plotL
	} else if closestX > plotR {
		closestX = plotR
	}
	closestY := cy
	if closestY < plotT {
		closestY = plotT
	} else if closestY > plotB {
		closestY = plotB
	}
	dx := cx - closestX
	dy := cy - closestY
	return dx*dx+dy*dy <= radius*radius
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
	if rx < 0.0001 || ry < 0.0001 {
		return false
	}
	// Unit-circle space closest-point (correct for rx ≠ ry). Euclidean clamp misses diagonals.
	lx := (left - ex) / rx
	rxn := (right - ex) / rx
	ty := (top - ey) / ry
	by := (bottom - ey) / ry
	minX, maxX := lx, rxn
	if minX > maxX {
		minX, maxX = maxX, minX
	}
	minY, maxY := ty, by
	if minY > maxY {
		minY, maxY = maxY, minY
	}
	cx := 0.0
	if cx < minX {
		cx = minX
	} else if cx > maxX {
		cx = maxX
	}
	cy := 0.0
	if cy < minY {
		cy = minY
	} else if cy > maxY {
		cy = maxY
	}
	return cx*cx+cy*cy <= 1
}

func pushEllipseOutOfRect(ex, ey, rx, ry, left, right, top, bottom float64) (float64, float64, bool) {
	if !ellipseOverlapsRect(ex, ey, rx, ry, left, right, top, bottom) {
		return ex, ey, false
	}
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
	if dx*dx+dy*dy < 0.0001 {
		pushLeft := ex - left
		pushRight := right - ex
		pushUp := ey - top
		pushDown := bottom - ey
		best := pushLeft
		dx, dy = -1, 0
		if pushRight < best {
			best = pushRight
			dx, dy = 1, 0
		}
		if pushUp < best {
			best = pushUp
			dx, dy = 0, -1
		}
		if pushDown < best {
			dx, dy = 0, 1
		}
		// Prefer north when nearly tied — south flings past thin tree stems.
		if math.Abs(pushUp-pushDown) < 0.5 && pushUp <= pushLeft && pushUp <= pushRight {
			dx, dy = 0, -1
		}
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

	// Soft depenetrate if already inside. Cap distance — never fling past a thin stem.
	if idx.feetWouldCollide(fromX, fromY, entityRx, entityRy) {
		newX, newY := idx.resolvePosition(fromX, fromY, entityRx, entityRy)
		dx := newX - fromX
		dy := newY - fromY
		maxPush := math.Max(entityRx, entityRy) + 4
		distSq := dx*dx + dy*dy
		if distSq > maxPush*maxPush && distSq > 0.0001 {
			scale := maxPush / math.Sqrt(distSq)
			dx *= scale
			dy *= scale
			newX = fromX + dx
			newY = fromY + dy
		}
		fromX, fromY = newX, newY
		// Do not shift to by the same delta (that recreated south-side teleports).
	}

	fromCy := playerCollisionY(fromY)
	toCy := playerCollisionY(toY)
	if idx.pathClear(fromX, fromCy, toX, toCy, entityRx, entityRy) {
		return toX, toY
	}

	ax, ay := idx.tryFoliageSlide(fromX, fromY, toX, toY, entityRx, entityRy, true)
	bx, by := idx.tryFoliageSlide(fromX, fromY, toX, toY, entityRx, entityRy, false)
	px, py := idx.binaryClampPath(fromX, fromY, toX, toY, entityRx, entityRy)
	da := (ax-fromX)*(ax-fromX) + (ay-fromY)*(ay-fromY)
	db := (bx-fromX)*(bx-fromX) + (by-fromY)*(by-fromY)
	dp := (px-fromX)*(px-fromX) + (py-fromY)*(py-fromY)
	bestX, bestY, bestD := px, py, dp
	if da > bestD {
		bestX, bestY, bestD = ax, ay, da
	}
	if db > bestD {
		bestX, bestY, bestD = bx, by, db
	}
	if bestD > 0.0001 {
		return bestX, bestY
	}
	return fromX, fromY
}

// pathClear samples along from→to so thin tree stems cannot be tunneled in one frame.
func (idx *foliageIndex) pathClear(fromX, fromCy, toX, toCy, entityRx, entityRy float64) bool {
	fromFeetY := feetFromCollisionY(fromCy)
	toFeetY := feetFromCollisionY(toCy)
	if idx.feetWouldCollide(fromX, fromFeetY, entityRx, entityRy) ||
		idx.feetWouldCollide(toX, toFeetY, entityRx, entityRy) {
		return false
	}
	dx := toX - fromX
	dy := toCy - fromCy
	dist := math.Sqrt(dx*dx + dy*dy)
	if dist < 0.001 {
		return true
	}
	steps := int(math.Ceil(dist / 2))
	if steps < 2 {
		steps = 2
	}
	for i := 1; i < steps; i++ {
		t := float64(i) / float64(steps)
		mx := fromX + dx*t
		my := fromCy + dy*t
		if idx.feetWouldCollide(mx, feetFromCollisionY(my), entityRx, entityRy) {
			return false
		}
	}
	return true
}

func (idx *foliageIndex) binaryClampPath(fromX, fromY, toX, toY, entityRx, entityRy float64) (float64, float64) {
	fromCy := playerCollisionY(fromY)
	toCy := playerCollisionY(toY)
	if idx.pathClear(fromX, fromCy, toX, toCy, entityRx, entityRy) {
		return toX, toY
	}
	bestX, bestY := fromX, fromY
	lo, hi := 0.0, 1.0
	for i := 0; i < 8; i++ {
		mid := (lo + hi) * 0.5
		mx := fromX + (toX-fromX)*mid
		my := fromY + (toY-fromY)*mid
		mCy := playerCollisionY(my)
		if !idx.feetWouldCollide(mx, my, entityRx, entityRy) &&
			idx.pathClear(fromX, fromCy, mx, mCy, entityRx, entityRy) {
			bestX, bestY = mx, my
			lo = mid
		} else {
			hi = mid
		}
	}
	return bestX, bestY
}

func (idx *foliageIndex) tryFoliageSlide(fromX, fromY, toX, toY, entityRx, entityRy float64, xFirst bool) (float64, float64) {
	x, y := fromX, fromY
	if xFirst {
		x, y = idx.binaryClampPath(fromX, fromY, toX, fromY, entityRx, entityRy)
		x, y = idx.binaryClampPath(x, y, x, toY, entityRx, entityRy)
	} else {
		x, y = idx.binaryClampPath(fromX, fromY, fromX, toY, entityRx, entityRy)
		x, y = idx.binaryClampPath(x, y, toX, y, entityRx, entityRy)
	}
	return x, y
}

func (idx *foliageIndex) feetWouldCollide(x, y, entityRx, entityRy float64) bool {
	if idx == nil {
		return false
	}
	cy := playerCollisionY(y)
	pad := math.Max(entityRx, entityRy) + idx.queryPad
	return idx.forEachNear(x, cy, pad, func(_ int, f *foliageCircle) bool {
		if f.trunkSortY > 0 && y < f.trunkSortY {
			return false
		}
		if f.squareHalf > 0 {
			top := f.squareBottom - f.squareHeight
			return ellipseOverlapsRect(x, cy, entityRx, entityRy, f.x-f.squareHalf, f.x+f.squareHalf, top, f.squareBottom)
		}
		return ellipseOverlapsCircle(x, cy, entityRx, entityRy, f.x, f.y, f.radius)
	})
}

func (idx *foliageIndex) resolvePosition(x, y, entityRx, entityRy float64) (float64, float64) {
	if idx == nil {
		return x, y
	}
	cy := playerCollisionY(y)
	pad := math.Max(entityRx, entityRy) + idx.queryPad
	for iter := 0; iter < 4; iter++ {
		pushed := false
		minCx := int(math.Floor((x - pad) / idx.cellSize))
		maxCx := int(math.Floor((x + pad) / idx.cellSize))
		minCy := int(math.Floor((cy - pad) / idx.cellSize))
		maxCy := int(math.Floor((cy + pad) / idx.cellSize))
		for cellY := minCy; cellY <= maxCy; cellY++ {
			for cellX := minCx; cellX <= maxCx; cellX++ {
				list := idx.cells[[2]int{cellX, cellY}]
				for _, i := range list {
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
			}
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
