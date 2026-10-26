package game

import (
	_ "embed"
	"encoding/json"
	"log"

	"github.com/deathborn/server/internal/worldmap"
)

const safeExclusionPad = 96.0

// Authoritative file: shared/zones.json (sync with: py -3 scripts/sync_shared_embeds.py).
//
//go:embed zones.json
var zonesJSON []byte

// zoneSpec is one entry in zones.json. Sizes are in tiles; NewZoneIndex converts them.
type zoneSpec struct {
	ID         string     `json:"id"`
	Name       string     `json:"name"`
	Kind       string     `json:"kind"`
	Safe       bool       `json:"safe"`
	Anchor     string     `json:"anchor"`     // "spawn" centres on the default spawn point
	CenterTile [2]float64 `json:"centerTile"` // used when Anchor is not "spawn"
	HalfWTiles float64    `json:"halfWTiles"`
	HalfHTiles float64    `json:"halfHTiles"`
}

type zonesFile struct {
	Zones []zoneSpec `json:"zones"`
}

type zoneDef struct {
	id, name string
	kind     string
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

// ZoneInfo is a zone in world pixels, as sent to clients.
type ZoneInfo struct {
	ID    string  `json:"id"`
	Name  string  `json:"name"`
	Kind  string  `json:"kind"`
	Safe  bool    `json:"safe"`
	X     float64 `json:"x"`
	Y     float64 `json:"y"`
	HalfW float64 `json:"halfW"`
	HalfH float64 `json:"halfH"`
}

// NewZoneIndex builds the zone index from the embedded zones.json. A bad file
// logs and leaves the world with no safe zones, rather than crashing the server.
func NewZoneIndex(terrain *worldmap.Map) *ZoneIndex {
	z := &ZoneIndex{exclusionPad: safeExclusionPad}
	if terrain == nil {
		return z
	}
	var file zonesFile
	if err := json.Unmarshal(zonesJSON, &file); err != nil {
		log.Printf("zones.json: %v (no zones loaded)", err)
		return z
	}

	ts := terrain.TileSize
	scale := ts / 16.0
	z.exclusionPad = safeExclusionPad * scale
	for _, s := range file.Zones {
		cx, cy := terrain.DefaultSpawnX, terrain.DefaultSpawnY
		if s.Anchor != "spawn" {
			cx = (s.CenterTile[0] + 0.5) * ts
			cy = (s.CenterTile[1] + 0.5) * ts
		}
		z.zones = append(z.zones, zoneDef{
			id: s.ID, name: s.Name, kind: s.Kind, safe: s.Safe,
			centerX: cx, centerY: cy,
			halfW: s.HalfWTiles * ts, halfH: s.HalfHTiles * ts,
		})
	}
	return z
}

// ZoneList returns every zone in world pixels, in file order.
func (w *World) ZoneList() []ZoneInfo {
	if w.zones == nil {
		return nil
	}
	out := make([]ZoneInfo, 0, len(w.zones.zones))
	for _, zd := range w.zones.zones {
		out = append(out, ZoneInfo{
			ID: zd.id, Name: zd.name, Kind: zd.kind, Safe: zd.safe,
			X: zd.centerX, Y: zd.centerY, HalfW: zd.halfW, HalfH: zd.halfH,
		})
	}
	return out
}

// ZoneAt returns the zone containing a position, if any.
func (w *World) ZoneAt(x, y float64) (ZoneInfo, bool) {
	if w.zones == nil {
		return ZoneInfo{}, false
	}
	zd := w.zones.At(x, y)
	if zd == nil {
		return ZoneInfo{}, false
	}
	return ZoneInfo{
		ID: zd.id, Name: zd.name, Kind: zd.kind, Safe: zd.safe,
		X: zd.centerX, Y: zd.centerY, HalfW: zd.halfW, HalfH: zd.halfH,
	}, true
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
