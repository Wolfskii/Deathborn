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

// Map is a tile walkability grid for the Realik continent.
type Map struct {
	TileWidth, TileHeight int
	TileSize              float64
	WorldWidth            float64
	WorldHeight           float64
	DefaultSpawnX         float64
	DefaultSpawnY         float64
	walkable              []bool
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

// CanWalk reports whether a circle at (x,y) may stand on land.
func (m *Map) CanWalk(x, y, radius float64) bool {
	if x < radius || y < radius || x > m.WorldWidth-radius || y > m.WorldHeight-radius {
		return false
	}
	if radius <= 0 {
		return m.walkTile(x, y)
	}
	return m.walkTile(x, y) &&
		m.walkTile(x+radius, y) &&
		m.walkTile(x-radius, y) &&
		m.walkTile(x, y+radius) &&
		m.walkTile(x, y-radius)
}

// ResolveMove applies axis-separated sliding against land/water tiles and foliage.
func (m *Map) ResolveMove(x, y, dx, dy float64) (float64, float64) {
	nx, ny := x+dx, y+dy
	if m.CanWalk(nx, y, playerRadius) {
		x = nx
	}
	if m.CanWalk(x, ny, playerRadius) {
		y = ny
	}
	if m.foliage != nil {
		x, y = m.foliage.resolvePosition(x, y, playerRadius)
	}
	return x, y
}
