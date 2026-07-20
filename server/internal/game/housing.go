package game

import (
	"fmt"
	"math"

	"github.com/deathborn/server/internal/db"
)

const (
	PlotHalfW          = 200.0
	PlotHalfH          = 176.0
	HouseHalfW         = 104.0
	HouseHalfH         = 88.0
	InteriorHalfW      = 420.0
	InteriorHalfH      = 250.0
	HouseMinSeparation = 440.0
	HouseMaxPlaceDist  = 320.0
	MaxFurniture       = 24

	// Exterior cottage collision (matches client HousingCollision).
	houseBodyHalfW      = 50.0
	houseBodyTop        = -108.0
	houseBodyBottom     = -4.0
	houseDoorGapHalfW   = 20.0
	houseDoorApproachS  = 36.0
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
	return x >= centerX-InteriorHalfW && x <= centerX+InteriorHalfW &&
		y >= centerY-InteriorHalfH && y <= centerY+InteriorHalfH-16
}

// GardenCropOffsets returns local offsets for 6 farm tiles in the garden.
func GardenCropOffsets() [][2]float64 {
	return [][2]float64{
		{-96, 104}, {0, 116}, {96, 104},
		{-96, 156}, {0, 168}, {96, 156},
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

// TooFarToPlace reports whether build coords are beyond the player's placement reach.
func TooFarToPlace(playerX, playerY, buildX, buildY float64) bool {
	dx := buildX - playerX
	dy := buildY - playerY
	return dx*dx+dy*dy > HouseMaxPlaceDist*HouseMaxPlaceDist
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
		{cx - PlotHalfW*0.85, cy - PlotHalfH*0.85},
		{cx + PlotHalfW*0.85, cy - PlotHalfH*0.85},
		{cx - PlotHalfW*0.85, cy + PlotHalfH*0.85},
		{cx + PlotHalfW*0.85, cy + PlotHalfH*0.85},
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

func (h *HousingIndex) RemoveByID(houseID int64) *housePlot {
	p := h.byID[houseID]
	if p == nil {
		return nil
	}
	delete(h.byID, houseID)
	delete(h.byCharacter, p.characterID)
	return p
}

func (h *HousingIndex) TransferOwnership(houseID, newCharacterID int64, newOwnerName string) *housePlot {
	p := h.byID[houseID]
	if p == nil {
		return nil
	}
	delete(h.byCharacter, p.characterID)
	p.characterID = newCharacterID
	p.ownerName = newOwnerName
	h.byCharacter[newCharacterID] = p
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
		if math.Hypot(f.X-item.X, f.Y-item.Y) < 36 {
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

// Door and interior layout (matches client FarmRpgHouseSprites orange cottage).
// Door is on the right facade; offsets are art px × DisplayScale (1.55).
func HouseDoorPosition(centerX, centerY float64) (float64, float64) {
	const displayScale = 1.55
	const artW = 72.0
	const artH = 86.0
	return centerX + artW*0.22*displayScale, centerY - artH*0.12*displayScale
}

func HouseInteriorSpawn(centerX, centerY float64) (float64, float64) {
	return centerX, centerY - 24
}

func HouseInteriorDoorPosition(centerX, centerY float64) (float64, float64) {
	return centerX, centerY + InteriorHalfH - 40
}

func HouseExteriorSpawn(centerX, centerY float64) (float64, float64) {
	dx, dy := HouseDoorPosition(centerX, centerY)
	return dx, dy + 36
}

const houseTransitionCooldownSec = 1.1

func (p *player) tickHouseTransitionCooldown(dt float64) {
	if p.houseTransitionCooldown > 0 {
		p.houseTransitionCooldown -= dt
	}
}

func (p *player) lockHouseTransition() {
	p.houseTransitionCooldown = houseTransitionCooldownSec
}

func canAutoHouseTransition(p *player) bool {
	return p.houseTransitionCooldown <= 0
}

func NearHouseDoor(x, y, centerX, centerY float64) bool {
	dx, dy := HouseDoorPosition(centerX, centerY)
	return math.Hypot(x-dx, y-dy) <= 76
}

func NearInteriorExit(x, y, centerX, centerY float64) bool {
	dx, dy := HouseInteriorDoorPosition(centerX, centerY)
	if y < dy-28 {
		return false
	}
	return math.Hypot(x-dx, y-dy) <= 52
}

func clampToInterior(x, y, centerX, centerY float64) (float64, float64) {
	const inset = 32.0
	minX := centerX - InteriorHalfW + inset
	maxX := centerX + InteriorHalfW - inset
	minY := centerY - InteriorHalfH + inset
	maxY := centerY + InteriorHalfH - 16 - inset
	if x < minX {
		x = minX
	}
	if x > maxX {
		x = maxX
	}
	if y < minY {
		y = minY
	}
	if y > maxY {
		y = maxY
	}
	return x, y
}

func inDoorApproach(x, y, centerX, centerY float64) bool {
	dx, _ := HouseDoorPosition(centerX, centerY)
	bodyBottom := centerY + houseBodyBottom
	return x >= dx-houseDoorGapHalfW && x <= dx+houseDoorGapHalfW &&
		y >= bodyBottom-6 && y <= bodyBottom+houseDoorApproachS
}

func overlapsHouseBody(x, y, centerX, centerY float64) bool {
	if inDoorApproach(x, y, centerX, centerY) {
		return false
	}
	left := centerX - houseBodyHalfW
	right := centerX + houseBodyHalfW
	top := centerY + houseBodyTop
	bottom := centerY + houseBodyBottom
	dx, _ := HouseDoorPosition(centerX, centerY)
	gapL := dx - houseDoorGapHalfW
	gapR := dx + houseDoorGapHalfW
	sill := bottom - 10

	rx, ry := 12.0, 16.0
	cy := y - 7*1.5*1.35 - ry + 2 // match worldmap playerCollisionY approx
	if ellipseOverlapsHouseRect(x, cy, rx, ry, left, right, top, sill) {
		return true
	}
	if ellipseOverlapsHouseRect(x, cy, rx, ry, left, gapL, sill, bottom) {
		return true
	}
	if ellipseOverlapsHouseRect(x, cy, rx, ry, gapR, right, sill, bottom) {
		return true
	}
	return false
}

func ellipseOverlapsHouseRect(ex, ey, rx, ry, left, right, top, bottom float64) bool {
	// Closest point on rect to ellipse center, then unit-circle test.
	cx := ex
	if cx < left {
		cx = left
	} else if cx > right {
		cx = right
	}
	cy := ey
	if cy < top {
		cy = top
	} else if cy > bottom {
		cy = bottom
	}
	nx := (cx - ex) / rx
	ny := (cy - ey) / ry
	return nx*nx+ny*ny <= 1
}

func (h *HousingIndex) blocksFeet(x, y float64) bool {
	for _, p := range h.byID {
		if overlapsHouseBody(x, y, p.centerX, p.centerY) {
			return true
		}
	}
	return false
}

// ResolveAgainstHouses slides a move so players cannot walk through cottage solids.
func (h *HousingIndex) ResolveAgainstHouses(fromX, fromY, toX, toY float64) (float64, float64) {
	if h == nil || len(h.byID) == 0 {
		return toX, toY
	}
	if !h.blocksFeet(toX, toY) {
		return toX, toY
	}
	ax, ay := toX, fromY
	bx, by := fromX, toY
	aOk := !h.blocksFeet(ax, ay)
	bOk := !h.blocksFeet(bx, by)
	if aOk && bOk {
		da := (ax-fromX)*(ax-fromX) + (ay-fromY)*(ay-fromY)
		db := (bx-fromX)*(bx-fromX) + (by-fromY)*(by-fromY)
		if da >= db {
			return ax, ay
		}
		return bx, by
	}
	if aOk {
		return ax, ay
	}
	if bOk {
		return bx, by
	}
	lo, hi := 0.0, 1.0
	bestX, bestY := fromX, fromY
	for i := 0; i < 8; i++ {
		mid := (lo + hi) * 0.5
		mx := fromX + (toX-fromX)*mid
		my := fromY + (toY-fromY)*mid
		if !h.blocksFeet(mx, my) {
			bestX, bestY = mx, my
			lo = mid
		} else {
			hi = mid
		}
	}
	return bestX, bestY
}

func interiorWallRects(centerX, centerY float64) [][4]float64 {
	const t = 18.0
	hw := InteriorHalfW
	hh := InteriorHalfH
	doorX, doorY := HouseInteriorDoorPosition(centerX, centerY)
	_ = doorY
	exitHalf := 28.0
	midL := centerX - hw/3
	midR := centerX + hw/3
	openHalf := 26.0
	openY := centerY
	return [][4]float64{
		{centerX - hw, centerX + hw, centerY - hh, centerY - hh + t},
		{centerX - hw, centerX - hw + t, centerY - hh, centerY + hh},
		{centerX + hw - t, centerX + hw, centerY - hh, centerY + hh},
		{centerX - hw, doorX - exitHalf, centerY + hh - t - 8, centerY + hh - 8},
		{doorX + exitHalf, centerX + hw, centerY + hh - t - 8, centerY + hh - 8},
		{midL - t*0.5, midL + t*0.5, centerY - hh, openY - openHalf},
		{midL - t*0.5, midL + t*0.5, openY + openHalf, centerY + hh - 8},
		{midR - t*0.5, midR + t*0.5, centerY - hh, openY - openHalf},
		{midR - t*0.5, midR + t*0.5, openY + openHalf, centerY + hh - 8},
	}
}

func feetHitWalls(x, y float64, walls [][4]float64) bool {
	rx, ry := 12.0, 16.0
	cy := y - 7*1.5*1.35 - ry + 2
	for _, w := range walls {
		if ellipseOverlapsHouseRect(x, cy, rx, ry, w[0], w[1], w[2], w[3]) {
			return true
		}
	}
	return false
}

func resolveInteriorMove(fromX, fromY, toX, toY, centerX, centerY float64) (float64, float64) {
	toX, toY = clampToInterior(toX, toY, centerX, centerY)
	walls := interiorWallRects(centerX, centerY)
	if !feetHitWalls(toX, toY, walls) {
		return toX, toY
	}
	ax, ay := clampToInterior(toX, fromY, centerX, centerY)
	bx, by := clampToInterior(fromX, toY, centerX, centerY)
	aOk := !feetHitWalls(ax, ay, walls)
	bOk := !feetHitWalls(bx, by, walls)
	if aOk && bOk {
		da := (ax-fromX)*(ax-fromX) + (ay-fromY)*(ay-fromY)
		db := (bx-fromX)*(bx-fromX) + (by-fromY)*(by-fromY)
		if da >= db {
			return ax, ay
		}
		return bx, by
	}
	if aOk {
		return ax, ay
	}
	if bOk {
		return bx, by
	}
	return fromX, fromY
}
