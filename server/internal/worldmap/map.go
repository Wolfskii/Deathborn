package worldmap

import (
	_ "embed"
	"encoding/binary"
	"fmt"
	"math"
)

//go:embed realik_collision.bin
var collisionData []byte

const playerRadius = 12.0
const playerCollisionYOffset = 2.0

func playerCollisionY(y float64) float64 { return y + playerCollisionYOffset }

// Map is a tile walkability grid for the Realik continent.
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
	return m.elevation.canStep(fx, fy, tx, ty, m.TileWidth, m.TileHeight)
}

func (m *Map) canTraverseWorld(fromX, fromY, toX, toY float64) bool {
	fx, fy := m.tileAt(fromX, fromY)
	tx, ty := m.tileAt(toX, toY)
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

// CanWalk reports whether a circle at (x,y) may stand on land. (x,y) is the feet position.
func (m *Map) CanWalk(x, y, radius float64) bool {
	cy := playerCollisionY(y)
	if x < radius || cy < radius || x > m.WorldWidth-radius || cy > m.WorldHeight-radius {
		return false
	}
	if radius <= 0 {
		return m.walkTile(x, cy)
	}
	return m.walkTile(x, cy) &&
		m.walkTile(x+radius, cy) &&
		m.walkTile(x-radius, cy) &&
		m.walkTile(x, cy+radius) &&
		m.walkTile(x, cy-radius)
}

// ResolveMove applies axis-separated sliding against land/water tiles and foliage.
func (m *Map) ResolveMove(x, y, dx, dy float64) (float64, float64) {
	fromX, fromY := x, y
	dx, dy = m.adjustRampDelta(x, y, dx, dy)

	nx, ny := x+dx, y+dy
	if m.CanWalk(nx, ny, playerRadius) && m.canTraverseWorld(x, y, nx, ny) {
		x, y = nx, ny
	} else {
		if m.CanWalk(nx, y, playerRadius) && m.canTraverseWorld(x, y, nx, y) {
			x = nx
		}
		if m.CanWalk(x, ny, playerRadius) && m.canTraverseWorld(x, y, x, ny) {
			y = ny
		}
	}
	if m.foliage != nil {
		x, y = m.foliage.resolveMoveBlock(fromX, fromY, x, y, playerRadius)
	}
	return x, y
}
