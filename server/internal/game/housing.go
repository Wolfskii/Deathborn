package game

import (
	"fmt"
	"math"

	"github.com/deathborn/server/internal/db"
)

const (
	PlotHalfW          = 100.0
	PlotHalfH          = 88.0
	HouseHalfW         = 52.0
	HouseHalfH         = 44.0
	InteriorHalfW      = 210.0
	InteriorHalfH      = 125.0
	HouseMinSeparation = 220.0
	HouseMaxPlaceDist  = 160.0
	MaxFurniture       = 24

	// Exterior cottage collision (matches client HousingCollision).
	houseBodyHalfW     = 50.0
	houseBodyBottom    = -4.0
	houseRoofEave      = -64.0
	houseRoofApex      = -108.0
	houseDoorGapHalfW  = 20.0
	houseDoorApproachS = 36.0

	// Homestead fence AABBs (matches client HomesteadFence).
	fenceThickness = 14.0
	fenceWorldTile = 16.0
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
	if msg := plotOverlapsOccupants(w, x, y); msg != "" {
		return msg
	}
	if w.terrain != nil {
		if w.terrain.PlotOverlapsFoliage(x, y, PlotHalfW, PlotHalfH) {
			return "Clear tree trunks from the plot first."
		}
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
	// Just inside the south exit door — player appears at the doorway and walks up into the room.
	dx, dy := HouseInteriorDoorPosition(centerX, centerY)
	return dx, dy - 18
}

func HouseInteriorDoorPosition(centerX, centerY float64) (float64, float64) {
	return centerX, centerY + InteriorHalfH - 20
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
	return math.Hypot(x-dx, y-dy) <= 38
}

func NearInteriorExit(x, y, centerX, centerY float64) bool {
	dx, dy := HouseInteriorDoorPosition(centerX, centerY)
	if y < dy-14 {
		return false
	}
	return math.Hypot(x-dx, y-dy) <= 26
}

func clampToInterior(x, y, centerX, centerY float64) (float64, float64) {
	const inset = 16.0
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

func plotOverlapsOccupants(w *World, centerX, centerY float64) string {
	for _, p := range w.players {
		if p.dead {
			continue
		}
		if entityOverlapsPlot(p.x, p.y, 12, 16, centerX, centerY) {
			return "Move players and creatures out of the plot first."
		}
	}
	if w.mobMgr != nil {
		for _, m := range w.mobMgr.mobs {
			rx := m.radius
			if rx < 4 {
				rx = 4
			}
			ry := rx * (16.0 / 12.0)
			if entityOverlapsPlot(m.x, m.y, rx, ry, centerX, centerY) {
				return "Clear creatures from the plot first."
			}
		}
	}
	return ""
}

func entityOverlapsPlot(feetX, feetY, rx, ry, centerX, centerY float64) bool {
	if overlapsHouseBodyForBuild(feetX, feetY, rx, ry, centerX, centerY) {
		return true
	}
	return overlapsHomesteadFenceEntity(feetX, feetY, rx, ry, centerX, centerY)
}

func playerCollisionY(feetY, ry float64) float64 {
	return feetY - 7*1.5*1.35 - ry + 2
}

func overlapsHouseBody(x, y, centerX, centerY float64) bool {
	return overlapsHouseBodyEx(x, y, 12, 16, centerX, centerY, true)
}

func overlapsHouseBodyForBuild(x, y, rx, ry, centerX, centerY float64) bool {
	return overlapsHouseBodyEx(x, y, rx, ry, centerX, centerY, false)
}

func overlapsHouseBodyEx(x, y, rx, ry, centerX, centerY float64, allowDoorApproach bool) bool {
	if allowDoorApproach && inDoorApproach(x, y, centerX, centerY) {
		return false
	}
	left := centerX - houseBodyHalfW
	right := centerX + houseBodyHalfW
	wallTop := centerY + houseRoofEave
	bottom := centerY + houseBodyBottom
	cy := playerCollisionY(y, ry)

	if ellipseOverlapsHouseRect(x, cy, rx, ry, left, right, wallTop, bottom) {
		return true
	}

	apexX, apexY := centerX, centerY+houseRoofApex
	return ellipseOverlapsHouseTriangle(x, cy, rx, ry,
		apexX, apexY,
		left, wallTop,
		right, wallTop)
}

func ellipseOverlapsHouseTriangle(ex, ey, rx, ry, ax, ay, bx, by, cx, cy float64) bool {
	if rx < 0.0001 || ry < 0.0001 {
		return false
	}
	toU := func(px, py float64) (float64, float64) {
		return (px - ex) / rx, (py - ey) / ry
	}
	tax, tay := toU(ax, ay)
	tbx, tby := toU(bx, by)
	tcx, tcy := toU(cx, cy)
	if pointInTriangle(0, 0, tax, tay, tbx, tby, tcx, tcy) {
		return true
	}
	cxp, cyp := closestOnTriangle(0, 0, tax, tay, tbx, tby, tcx, tcy)
	return cxp*cxp+cyp*cyp <= 1
}

func pointInTriangle(px, py, ax, ay, bx, by, cx, cy float64) bool {
	v0x, v0y := cx-ax, cy-ay
	v1x, v1y := bx-ax, by-ay
	v2x, v2y := px-ax, py-ay
	dot00 := v0x*v0x + v0y*v0y
	dot01 := v0x*v1x + v0y*v1y
	dot02 := v0x*v2x + v0y*v2y
	dot11 := v1x*v1x + v1y*v1y
	dot12 := v1x*v2x + v1y*v2y
	denom := dot00*dot11 - dot01*dot01
	if math.Abs(denom) < 1e-12 {
		return false
	}
	u := (dot11*dot02 - dot01*dot12) / denom
	v := (dot00*dot12 - dot01*dot02) / denom
	return u >= 0 && v >= 0 && u+v <= 1
}

func closestOnTriangle(px, py, ax, ay, bx, by, cx, cy float64) (float64, float64) {
	abx, aby := closestOnSegment(px, py, ax, ay, bx, by)
	bcx, bcy := closestOnSegment(px, py, bx, by, cx, cy)
	cax, cay := closestOnSegment(px, py, cx, cy, ax, ay)
	dab := (px-abx)*(px-abx) + (py-aby)*(py-aby)
	dbc := (px-bcx)*(px-bcx) + (py-bcy)*(py-bcy)
	dca := (px-cax)*(px-cax) + (py-cay)*(py-cay)
	if dab <= dbc && dab <= dca {
		return abx, aby
	}
	if dbc <= dca {
		return bcx, bcy
	}
	return cax, cay
}

func closestOnSegment(px, py, ax, ay, bx, by float64) (float64, float64) {
	abx, aby := bx-ax, by-ay
	lenSq := abx*abx + aby*aby
	if lenSq < 1e-8 {
		return ax, ay
	}
	t := ((px-ax)*abx + (py-ay)*aby) / lenSq
	if t < 0 {
		t = 0
	} else if t > 1 {
		t = 1
	}
	return ax + abx*t, ay + aby*t
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
		if overlapsHomesteadFence(x, y, p.centerX, p.centerY) {
			return true
		}
	}
	return false
}

// overlapsHomesteadFence — five thin AABBs around the plot (south gate gap open).
func overlapsHomesteadFence(x, y, centerX, centerY float64) bool {
	return overlapsHomesteadFenceEntity(x, y, 12, 16, centerX, centerY)
}

func overlapsHomesteadFenceEntity(x, y, rx, ry, centerX, centerY float64) bool {
	left := centerX - PlotHalfW
	top := centerY - PlotHalfH
	spanX := PlotHalfW * 2
	spanY := PlotHalfH * 2
	right := left + spanX
	bottom := top + spanY

	nx := int(math.Round(spanX / fenceWorldTile))
	if nx < 4 {
		nx = 4
	}
	spacingX := spanX / float64(nx)
	gateL := nx/2 - 1
	if gateL < 1 {
		gateL = 1
	}
	if gateL > nx-3 {
		gateL = nx - 3
	}
	gateR := gateL + 2
	gapL := left + float64(gateL)*spacingX
	gapR := left + float64(gateR)*spacingX
	t := fenceThickness * 0.5

	cy := playerCollisionY(y, ry)

	rects := [5][4]float64{
		{left - t, right + t, top - t, top + t},     // north
		{left - t, left + t, top - t, bottom + t},   // west
		{right - t, right + t, top - t, bottom + t}, // east
		{left - t, gapL, bottom - t, bottom + t},    // south L
		{gapR, right + t, bottom - t, bottom + t},   // south R
	}
	for _, r := range rects {
		if ellipseOverlapsHouseRect(x, cy, rx, ry, r[0], r[1], r[2], r[3]) {
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
