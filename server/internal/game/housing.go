package game

import (
	"fmt"
	"math"

	"github.com/deathborn/server/internal/db"
)

const (
	PlotHalfW        = 100.0
	PlotHalfH        = 88.0
	HouseHalfW       = 52.0
	HouseHalfH       = 44.0
	HouseMinSeparation = 220.0
	MaxFurniture     = 24
)

// FurnitureItem is a placed interior object (world-relative coords).
type FurnitureItem struct {
	Type string  `json:"type"`
	X    float64 `json:"x"`
	Y    float64 `json:"y"`
}

// HouseState is the wire view of a player house.
type HouseState struct {
	ID        int64           `json:"id"`
	OwnerID   int64           `json:"ownerId"`
	OwnerName string          `json:"ownerName"`
	X         float64         `json:"x"`
	Y         float64         `json:"y"`
	Furniture []FurnitureItem `json:"furniture,omitempty"`
}

type housePlot struct {
	id          int64
	characterID int64
	ownerName   string
	centerX     float64
	centerY     float64
	furniture   []FurnitureItem
}

// HousingIndex tracks all player house safe zones.
type HousingIndex struct {
	byID        map[int64]*housePlot
	byCharacter map[int64]*housePlot
}

func NewHousingIndex() *HousingIndex {
	return &HousingIndex{
		byID:        make(map[int64]*housePlot),
		byCharacter: make(map[int64]*housePlot),
	}
}

func (h *HousingIndex) LoadFromDB(houses []db.House) {
	h.byID = make(map[int64]*housePlot)
	h.byCharacter = make(map[int64]*housePlot)
	for _, row := range houses {
		plot := dbHouseToPlot(row)
		h.byID[plot.id] = plot
		h.byCharacter[plot.characterID] = plot
	}
}

func dbHouseToPlot(row db.House) *housePlot {
	items := make([]FurnitureItem, 0, len(row.Furniture))
	for _, f := range row.Furniture {
		items = append(items, FurnitureItem{Type: f.Type, X: f.X, Y: f.Y})
	}
	return &housePlot{
		id: row.ID, characterID: row.CharacterID, ownerName: row.OwnerName,
		centerX: row.CenterX, centerY: row.CenterY, furniture: items,
	}
}

func (h *HousingIndex) Snapshot() []HouseState {
	out := make([]HouseState, 0, len(h.byID))
	for _, p := range h.byID {
		out = append(out, p.state())
	}
	return out
}

func (p *housePlot) state() HouseState {
	return HouseState{
		ID: p.id, OwnerID: p.characterID, OwnerName: p.ownerName,
		X: p.centerX, Y: p.centerY, Furniture: append([]FurnitureItem(nil), p.furniture...),
	}
}

func (h *HousingIndex) Contains(x, y float64) bool {
	return h.At(x, y) != nil
}

func (h *HousingIndex) At(x, y float64) *housePlot {
	for _, p := range h.byID {
		if x >= p.centerX-PlotHalfW && x <= p.centerX+PlotHalfW &&
			y >= p.centerY-PlotHalfH && y <= p.centerY+PlotHalfH {
			return p
		}
	}
	return nil
}

func (h *HousingIndex) ByCharacter(id int64) *housePlot {
	return h.byCharacter[id]
}

func (h *HousingIndex) PvPAllowed(x, y float64) bool {
	return !h.Contains(x, y)
}

func (h *HousingIndex) PvPAllowedBetween(ax, ay, tx, ty float64) bool {
	return h.PvPAllowed(ax, ay) && h.PvPAllowed(tx, ty)
}

func InHouseInterior(x, y float64, centerX, centerY float64) bool {
	return x >= centerX-HouseHalfW && x <= centerX+HouseHalfW &&
		y >= centerY-HouseHalfH-12 && y <= centerY+HouseHalfH-28
}

// GardenCropOffsets returns local offsets for 6 farm tiles in the garden.
func GardenCropOffsets() [][2]float64 {
	return [][2]float64{
		{-48, 52}, {0, 58}, {48, 52},
		{-48, 78}, {0, 84}, {48, 78},
	}
}

func (h *HousingIndex) CanBuildAt(w *World, characterID int64, x, y float64) string {
	if h.ByCharacter(characterID) != nil {
		return "You already have a house."
	}
	if w.zones != nil && w.zones.InMonsterExclusion(x, y) {
		return "Too close to a town — build farther out."
	}
	if h.overlapsExisting(x, y) {
		return "Too close to another house."
	}
	if !plotWalkable(w, x, y) {
		return "Terrain is not flat enough here."
	}
	return ""
}

func (h *HousingIndex) overlapsExisting(x, y float64) bool {
	minDist := HouseMinSeparation * HouseMinSeparation
	for _, p := range h.byID {
		dx := p.centerX - x
		dy := p.centerY - y
		if dx*dx+dy*dy < minDist {
			return true
		}
	}
	return false
}

func plotWalkable(w *World, cx, cy float64) bool {
	corners := [][2]float64{
		{cx - PlotHalfW * 0.85, cy - PlotHalfH * 0.85},
		{cx + PlotHalfW * 0.85, cy - PlotHalfH * 0.85},
		{cx - PlotHalfW * 0.85, cy + PlotHalfH * 0.85},
		{cx + PlotHalfW * 0.85, cy + PlotHalfH * 0.85},
		{cx, cy},
	}
	for _, c := range corners {
		if !w.CanWalk(c[0], c[1]) {
			return false
		}
	}
	return true
}

func (h *HousingIndex) Add(plot *housePlot) {
	h.byID[plot.id] = plot
	h.byCharacter[plot.characterID] = plot
}

func (h *HousingIndex) RemoveByCharacter(characterID int64) *housePlot {
	p := h.byCharacter[characterID]
	if p == nil {
		return nil
	}
	delete(h.byID, p.id)
	delete(h.byCharacter, characterID)
	return p
}

func (h *HousingIndex) SetFurniture(characterID int64, items []FurnitureItem) bool {
	p := h.byCharacter[characterID]
	if p == nil {
		return false
	}
	if len(items) > MaxFurniture {
		items = items[:MaxFurniture]
	}
	p.furniture = append([]FurnitureItem(nil), items...)
	return true
}

func (h *HousingIndex) CanPlaceFurniture(characterID int64, item FurnitureItem) string {
	p := h.byCharacter[characterID]
	if p == nil {
		return "You do not have a house."
	}
	if len(p.furniture) >= MaxFurniture {
		return "House is full of furniture."
	}
	if !InHouseInterior(item.X, item.Y, p.centerX, p.centerY) {
		return "Furniture must go inside your house."
	}
	for _, f := range p.furniture {
		if math.Hypot(f.X-item.X, f.Y-item.Y) < 18 {
			return "Too close to other furniture."
		}
	}
	switch item.Type {
	case "bed", "table", "chair", "chest", "fireplace", "rug":
		return ""
	default:
		return "Unknown furniture type."
	}
}

func (h *HousingIndex) AddFurniture(characterID int64, item FurnitureItem) bool {
	p := h.byCharacter[characterID]
	if p == nil {
		return false
	}
	p.furniture = append(p.furniture, item)
	return true
}

func CropTargetID(houseID int64, index int) string {
	return fmt.Sprintf("house_%d_crop_%d", houseID, index)
}
