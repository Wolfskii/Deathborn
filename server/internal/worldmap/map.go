package worldmap

import (
	_ "embed"
	"encoding/binary"
	"fmt"
	"math"
)

//go:embed swarovia_mainland_collision.bin
var collisionData []byte

const playerRadiusX = 12.0

const playerRadiusY = 16.0

const playerCollisionVerticalExtraPx = 4.0

// Match client PlayerEntity: ellipse bottom on foot row (FarmRpg FootBottomInsetPx × draw scale).
const (
	playerFootBottomInsetPx       = 7.0
	playerSpriteDrawScale         = 1.5 * 1.35
	playerCollisionFineTuneDownPx = 2.0
)

func playerCollisionY(feetY float64) float64 {
	return feetY - playerFootBottomInsetPx*playerSpriteDrawScale - playerRadiusY + playerCollisionFineTuneDownPx
}

func feetFromCollisionY(cy float64) float64 {
	return cy + playerFootBottomInsetPx*playerSpriteDrawScale + playerRadiusY - playerCollisionFineTuneDownPx
}

func playerCollisionBottomY(feetY float64) float64 {
	return playerCollisionY(feetY) + playerRadiusY
}

func playerCollisionTopY(feetY float64) float64 {
	return playerCollisionY(feetY) - playerRadiusY
}

// Map is a tile walkability grid for the Swarovia mainland overworld.
type Map struct {
	TileWidth, TileHeight int
	TileSize              float64
	WorldWidth            float64
	WorldHeight           float64
	DefaultSpawnX         float64
	DefaultSpawnY         float64
	walkable              []bool
	elevation             *elevationGrid
	foliage               *foliageIndex
}

// LoadEmbedded parses the baked collision file shipped with the server.
func LoadEmbedded() (*Map, error) {
	return parse(collisionData)
}

func parse(data []byte) (*Map, error) {
	if len(data) < 13 || string(data[:4]) != "REAL" {
		return nil, fmt.Errorf("worldmap: invalid header")
	}
	if data[4] != 1 {
		return nil, fmt.Errorf("worldmap: unsupported version %d", data[4])
	}

	tw := int(binary.LittleEndian.Uint16(data[5:7]))
	th := int(binary.LittleEndian.Uint16(data[7:9]))
	tileSize := float64(math.Float32frombits(binary.LittleEndian.Uint32(data[9:13])))
	expected := 13 + tw*th
	if len(data) < expected {
		return nil, fmt.Errorf("worldmap: truncated (%d < %d)", len(data), expected)
	}

	walk := make([]bool, tw*th)
	for i := 0; i < len(walk); i++ {
		walk[i] = data[13+i] != 0
	}

	m := &Map{
		TileWidth:  tw,
		TileHeight: th,
		TileSize:   tileSize,
		walkable:   walk,
	}
	m.WorldWidth = float64(tw) * tileSize
	m.WorldHeight = float64(th) * tileSize
	elev, err := loadElevation(tw, th)
	if err != nil {
		return nil, err
	}
	m.elevation = elev
	tx, ty := findStarterCampTile(m)
	m.DefaultSpawnX, m.DefaultSpawnY = tileCenter(tx, ty, tileSize)
	m.foliage = m.buildFoliage()
	return m, nil
}

const starterClearanceTiles = 10

func findStarterCampTile(m *Map) (int, int) {
	minTx := int(float64(m.TileWidth) * 0.32)
	maxTx := int(float64(m.TileWidth) * 0.68)
	minTy := int(float64(m.TileHeight) * 0.18)
	maxTy := int(float64(m.TileHeight) * 0.38)

	bestScore := -1
	bestTx, bestTy := m.TileWidth/2, m.TileHeight/2

	for ty := minTy; ty <= maxTy; ty++ {
		for tx := minTx; tx <= maxTx; tx++ {
			if !m.isLand(tx, ty) || !m.hasClearance(tx, ty, starterClearanceTiles) {
				continue
			}
			score := m.landCount(tx, ty, 14)
			if score > bestScore {
				bestScore = score
				bestTx, bestTy = tx, ty
			}
		}
	}
	return bestTx, bestTy
}

func (m *Map) isLand(tx, ty int) bool {
	if tx < 0 || ty < 0 || tx >= m.TileWidth || ty >= m.TileHeight {
		return false
	}
	return m.walkable[ty*m.TileWidth+tx]
}

func (m *Map) hasClearance(tx, ty, radius int) bool {
	for dy := -radius; dy <= radius; dy++ {
		for dx := -radius; dx <= radius; dx++ {
			if dx*dx+dy*dy > radius*radius {
				continue
			}
			if !m.isLand(tx+dx, ty+dy) {
				return false
			}
		}
	}
	return true
}

func (m *Map) landCount(tx, ty, radius int) int {
	n := 0
	for dy := -radius; dy <= radius; dy++ {
		for dx := -radius; dx <= radius; dx++ {
			if m.isLand(tx+dx, ty+dy) {
				n++
			}
		}
	}
	return n
}

func tileCenter(tx, ty int, tileSize float64) (float64, float64) {
	return (float64(tx) + 0.5) * tileSize, (float64(ty) + 0.5) * tileSize
}

func (m *Map) walkTile(worldX, worldY float64) bool {
	tx := int(worldX / m.TileSize)
	ty := int(worldY / m.TileSize)
	if tx < 0 || ty < 0 || tx >= m.TileWidth || ty >= m.TileHeight {
		return false
	}
	return m.walkable[ty*m.TileWidth+tx]
}

func (m *Map) tileAt(worldX, worldY float64) (int, int) {
	return int(worldX / m.TileSize), int(worldY / m.TileSize)
}

func (m *Map) canTraverseTiles(fx, fy, tx, ty int) bool {
	if m.elevation == nil {
		return true
	}
	if m.elevation.canStep(fx, fy, tx, ty, m.TileWidth, m.TileHeight) {
		return true
	}
	// Southward step down one elevation band on walkable land (plateau → shoreline).
	dx := tx - fx
	dy := ty - fy
	if dx == 0 && dy == 1 && m.isLand(tx, ty) {
		fe := m.elevation.at(fx, fy, m.TileWidth, m.TileHeight)
		te := m.elevation.at(tx, ty, m.TileWidth, m.TileHeight)
		if fe >= 0 && te >= 0 && fe == te+1 {
			return true
		}
	}
	// Northward step up one elevation band on walkable land (shoreline → plateau).
	if dx == 0 && dy == -1 && m.isLand(tx, ty) {
		fe := m.elevation.at(fx, fy, m.TileWidth, m.TileHeight)
		te := m.elevation.at(tx, ty, m.TileWidth, m.TileHeight)
		if fe >= 0 && te >= 0 && te == fe+1 {
			return true
		}
	}
	return false
}

func (m *Map) canTraverseWorld(fromX, fromY, toX, toY float64) bool {
	sampleFromY := fromY
	sampleToY := toY
	// Match client: feet sit south of the ellipse. Horizontal moves must sample the
	// southern ellipse edge or sideways sliding fails after pressing a south shore
	// (feet on water tile, ellipse still on land).
	if toY > fromY+0.001 {
		sampleFromY = playerCollisionBottomY(fromY)
		sampleToY = playerCollisionBottomY(toY)
	} else if toY < fromY-0.001 {
		sampleFromY = playerCollisionTopY(fromY)
		sampleToY = playerCollisionTopY(toY)
	} else {
		sampleFromY = playerCollisionBottomY(fromY)
		sampleToY = playerCollisionBottomY(toY)
	}

	fx, fy := m.tileAt(fromX, sampleFromY)
	tx, ty := m.tileAt(toX, sampleToY)
	if fx == tx && fy == ty {
		return true
	}
	if m.elevation != nil {
		rx1, ry1, k1, ok1 := m.elevation.rampEngagedAtWorld(fromX, fromY, m.TileSize, m.TileWidth, m.TileHeight)
		rx2, ry2, k2, ok2 := m.elevation.rampEngagedAtWorld(toX, toY, m.TileSize, m.TileWidth, m.TileHeight)
		if ok1 && ok2 && rx1 == rx2 && ry1 == ry2 && k1 == k2 {
			fe := m.elevation.at(fx, fy, m.TileWidth, m.TileHeight)
			te := m.elevation.at(tx, ty, m.TileWidth, m.TileHeight)
			if fe >= 0 && te >= 0 {
				diff := int(fe) - int(te)
				if diff < 0 {
					diff = -diff
				}
				if diff <= 1 {
					return true
				}
			}
		}
	}
	if m.canTraverseTiles(fx, fy, tx, fy) && m.canTraverseTiles(tx, fy, tx, ty) {
		return true
	}
	if fx != tx && fy != ty {
		if m.canTraverseTiles(fx, fy, fx, ty) && m.canTraverseTiles(fx, ty, tx, ty) {
			return true
		}
	}
	return false
}

func (m *Map) adjustRampDelta(x, y, dx, dy float64) (float64, float64) {
	if m.elevation == nil || math.Abs(dx) < 0.0001 {
		return dx, dy
	}
	ok, slope := m.elevation.rampSlopeAtWorld(x, y, m.TileSize, m.TileWidth, m.TileHeight)
	if !ok {
		return dx, dy
	}
	// On stairs, sideways input always follows the ramp unless the player is clearly
	// moving vertically on purpose (e.g. stepping off with W/S).
	if math.Abs(dy) > math.Abs(dx)*1.25 {
		return dx, dy
	}
	return dx, slope * dx
}

// CanWalk reports whether the player ellipse at feet (x,y) may stand on land.
// Uses full ellipse vs blocked tile AABBs (not 5 axial samples) so convex corners
// cannot clip past the land/water edge.
func (m *Map) CanWalk(x, y, radius float64) bool {
	cy := playerCollisionY(y)
	maxR := math.Max(playerRadiusX, playerRadiusY)
	if x < maxR || cy < maxR || x > m.WorldWidth-maxR || cy > m.WorldHeight-maxR {
		return false
	}
	if radius <= 0 {
		return m.walkTile(x, cy)
	}
	return m.ellipseClearOfBlockedTiles(x, cy, playerRadiusX, playerRadiusY)
}

func (m *Map) ellipseClearOfBlockedTiles(cx, cy, rx, ry float64) bool {
	minTx := int(math.Floor((cx - rx) / m.TileSize))
	maxTx := int(math.Floor((cx + rx) / m.TileSize))
	minTy := int(math.Floor((cy - ry) / m.TileSize))
	maxTy := int(math.Floor((cy + ry) / m.TileSize))
	if minTx < 0 {
		minTx = 0
	}
	if minTy < 0 {
		minTy = 0
	}
	if maxTx >= m.TileWidth {
		maxTx = m.TileWidth - 1
	}
	if maxTy >= m.TileHeight {
		maxTy = m.TileHeight - 1
	}
	for ty := minTy; ty <= maxTy; ty++ {
		for tx := minTx; tx <= maxTx; tx++ {
			if m.walkable[ty*m.TileWidth+tx] {
				continue
			}
			left := float64(tx) * m.TileSize
			top := float64(ty) * m.TileSize
			if ellipseOverlapsRect(cx, cy, rx, ry, left, left+m.TileSize, top, top+m.TileSize) {
				return false
			}
		}
	}
	return true
}

// ResolveMove applies axis-separated sliding against land/water tiles and foliage.
func (m *Map) ResolveMove(x, y, dx, dy float64) (float64, float64) {
	fromX, fromY := x, y
	dx, dy = m.adjustRampDelta(x, y, dx, dy)

	nx, ny := x+dx, y+dy
	if m.CanWalk(nx, ny, playerRadiusX) && m.canTraverseWorld(x, y, nx, ny) {
		x, y = nx, ny
	} else {
		ax, ay := m.tryAxisSlide(fromX, fromY, nx, ny, true)
		bx, by := m.tryAxisSlide(fromX, fromY, nx, ny, false)
		da := (ax-fromX)*(ax-fromX) + (ay-fromY)*(ay-fromY)
		db := (bx-fromX)*(bx-fromX) + (by-fromY)*(by-fromY)
		if da >= db {
			x, y = ax, ay
		} else {
			x, y = bx, by
		}
		if (x-fromX)*(x-fromX)+(y-fromY)*(y-fromY) < 0.0001 && dx*dx+dy*dy > 0.0001 {
			x, y = m.binaryClampMove(fromX, fromY, nx, ny)
		}
	}
	if m.foliage != nil {
		x, y = m.foliage.resolveMoveBlock(fromX, fromY, x, y, playerRadiusX, playerRadiusY)
	}
	return x, y
}

func (m *Map) tryAxisSlide(fromX, fromY, nx, ny float64, xFirst bool) (float64, float64) {
	x, y := fromX, fromY
	if xFirst {
		if m.CanWalk(nx, fromY, playerRadiusX) && m.canTraverseWorld(fromX, fromY, nx, fromY) {
			x = nx
		}
		if m.CanWalk(x, ny, playerRadiusX) && m.canTraverseWorld(x, fromY, x, ny) {
			y = ny
		}
	} else {
		if m.CanWalk(fromX, ny, playerRadiusX) && m.canTraverseWorld(fromX, fromY, fromX, ny) {
			y = ny
		}
		if m.CanWalk(nx, y, playerRadiusX) && m.canTraverseWorld(fromX, y, nx, y) {
			x = nx
		}
	}
	return x, y
}

func (m *Map) binaryClampMove(fromX, fromY, toX, toY float64) (float64, float64) {
	x, y := fromX, fromY
	lo, hi := 0.0, 1.0
	for i := 0; i < 8; i++ {
		mid := (lo + hi) * 0.5
		mx := fromX + (toX-fromX)*mid
		my := fromY + (toY-fromY)*mid
		if m.CanWalk(mx, my, playerRadiusX) && m.canTraverseWorld(fromX, fromY, mx, my) {
			x, y = mx, my
			lo = mid
		} else {
			hi = mid
		}
	}
	return x, y
}

// PlotOverlapsFoliage reports whether wilderness foliage sprites intersect a homestead plot.
func (m *Map) PlotOverlapsFoliage(cx, cy, halfW, halfH float64) bool {
	if m == nil || m.foliage == nil {
		return false
	}
	return m.foliage.plotOverlaps(cx, cy, halfW, halfH)
}
