package game

import "github.com/deathborn/server/internal/worldmap"

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
	zones []zoneDef
}

func NewZoneIndex(terrain *worldmap.Map) *ZoneIndex {
	ts := terrain.TileSize
	tileCenter := func(tx, ty int) (float64, float64) {
		return (float64(tx)+0.5)*ts, (float64(ty)+0.5)*ts
	}
	nx, ny := tileCenter(234, 45)
	wx, wy := tileCenter(39, 189)
	ex, ey := tileCenter(229, 128)
	sx, sy := tileCenter(112, 281)

	return &ZoneIndex{
		zones: []zoneDef{
			{id: "starter_town", name: "Starter Town", safe: true,
				centerX: terrain.DefaultSpawnX, centerY: terrain.DefaultSpawnY, halfW: 148, halfH: 128},
			{id: "northhaven", name: "Northhaven", safe: true, centerX: nx, centerY: ny, halfW: 136, halfH: 118},
			{id: "westmere", name: "Westmere", safe: true, centerX: wx, centerY: wy, halfW: 128, halfH: 112},
			{id: "eastwatch", name: "Eastwatch", safe: true, centerX: ex, centerY: ey, halfW: 132, halfH: 116},
			{id: "southport", name: "Southport", safe: true, centerX: sx, centerY: sy, halfW: 140, halfH: 120},
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
