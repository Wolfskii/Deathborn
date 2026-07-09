package game

import "github.com/deathborn/server/internal/worldmap"

const safeExclusionPad = 96.0

type zoneDef struct {
	id, name string
	safe     bool
	centerX  float64
	centerY  float64
	halfW    float64
	halfH    float64
}

// ZoneIndex maps world positions to named regions.
type ZoneIndex struct {
	zones        []zoneDef
	exclusionPad float64
}

func NewZoneIndex(terrain *worldmap.Map) *ZoneIndex {
	ts := terrain.TileSize
	scale := ts / 16.0
	tileCenter := func(tx, ty int) (float64, float64) {
		return (float64(tx) + 0.5) * ts, (float64(ty) + 0.5) * ts
	}
	nx, ny := tileCenter(234, 45)
	wx, wy := tileCenter(39, 189)
	ex, ey := tileCenter(229, 128)
	sx, sy := tileCenter(112, 281)

	return &ZoneIndex{
		exclusionPad: safeExclusionPad * scale,
		zones: []zoneDef{
			{id: "starter_town", name: "Starter Town", safe: true,
				centerX: terrain.DefaultSpawnX, centerY: terrain.DefaultSpawnY, halfW: 148 * scale, halfH: 128 * scale},
			{id: "northhaven", name: "Northhaven", safe: true, centerX: nx, centerY: ny, halfW: 136 * scale, halfH: 118 * scale},
			{id: "westmere", name: "Westmere", safe: true, centerX: wx, centerY: wy, halfW: 128 * scale, halfH: 112 * scale},
			{id: "eastwatch", name: "Eastwatch", safe: true, centerX: ex, centerY: ey, halfW: 132 * scale, halfH: 116 * scale},
			{id: "southport", name: "Southport", safe: true, centerX: sx, centerY: sy, halfW: 140 * scale, halfH: 120 * scale},
		},
	}
}

func (z *ZoneIndex) At(x, y float64) *zoneDef {
	for i := range z.zones {
		zd := &z.zones[i]
		if x >= zd.centerX-zd.halfW && x <= zd.centerX+zd.halfW &&
			y >= zd.centerY-zd.halfH && y <= zd.centerY+zd.halfH {
			return zd
		}
	}
	return nil
}

// PvPAllowed reports whether player-vs-player damage may occur at a position.
func (z *ZoneIndex) PvPAllowed(x, y float64) bool {
	zd := z.At(x, y)
	return zd == nil || !zd.safe
}

// PvPAllowedBetween reports whether PvP may occur between two positions.
func (z *ZoneIndex) PvPAllowedBetween(ax, ay, tx, ty float64) bool {
	return z.PvPAllowed(ax, ay) && z.PvPAllowed(tx, ty)
}

// InSafeArea reports whether a position is inside a safe town.
func (z *ZoneIndex) InSafeArea(x, y float64) bool {
	zd := z.At(x, y)
	return zd != nil && zd.safe
}

// InMonsterExclusion reports whether monsters/bosses may not enter (town + padding).
func (z *ZoneIndex) InMonsterExclusion(x, y float64) bool {
	for i := range z.zones {
		zd := &z.zones[i]
		if !zd.safe {
			continue
		}
		if x >= zd.centerX-zd.halfW-z.exclusionPad && x <= zd.centerX+zd.halfW+z.exclusionPad &&
			y >= zd.centerY-zd.halfH-z.exclusionPad && y <= zd.centerY+zd.halfH+z.exclusionPad {
			return true
		}
	}
	return false
}

// PushOutOfMonsterExclusion nudges a point to the nearest edge outside exclusion zones.
func (z *ZoneIndex) PushOutOfMonsterExclusion(x, y float64) (float64, float64) {
	if !z.InMonsterExclusion(x, y) {
		return x, y
	}
	bestX, bestY := x, y
	bestDist := -1.0
	for i := range z.zones {
		zd := &z.zones[i]
		if !zd.safe {
			continue
		}
		minX := zd.centerX - zd.halfW - z.exclusionPad
		maxX := zd.centerX + zd.halfW + z.exclusionPad
		minY := zd.centerY - zd.halfH - z.exclusionPad
		maxY := zd.centerY + zd.halfH + z.exclusionPad
		candidates := [][2]float64{
			{minX - 8*(z.exclusionPad/safeExclusionPad), y},
			{maxX + 8*(z.exclusionPad/safeExclusionPad), y},
			{x, minY - 8*(z.exclusionPad/safeExclusionPad)},
			{x, maxY + 8*(z.exclusionPad/safeExclusionPad)},
		}
		for _, c := range candidates {
			if z.InMonsterExclusion(c[0], c[1]) {
				continue
			}
			d := (c[0]-x)*(c[0]-x) + (c[1]-y)*(c[1]-y)
			if bestDist < 0 || d < bestDist {
				bestDist = d
				bestX, bestY = c[0], c[1]
			}
		}
	}
	return bestX, bestY
}
