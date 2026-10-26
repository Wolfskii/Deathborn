package game

import (
	"fmt"
	"math"

	"github.com/deathborn/server/internal/db"
)

const (
	PlotHalfW          = 66.0 // two tiles wider than the original 50 (8px per side)
	PlotHalfH          = 60.0 // two tiles taller than the original 44
	HouseHalfW         = 26.0
	HouseHalfH         = 22.0
	InteriorHalfW      = 208.0 // 26 tiles (see interior_layout.go)
	InteriorHalfH      = 120.0 // 15 tiles
	HouseMinSeparation = 110.0
	HouseMaxPlaceDist  = 80.0
	MaxFurniture       = 40
	// FurnitureMinSpacing keeps standing pieces from stacking; rugs may sit under anything.
	FurnitureMinSpacing = 14.0

	// Exterior cottage collision (matches client HousingCollision).
	houseBodyHalfW     = 25.0
	houseBodyBottom    = 2.0
	houseRoofEave      = -20.0
	houseRoofApex      = -54.0
	houseDoorGapHalfW  = 10.0
	houseDoorApproachS = 30.0 // reaches past where the player's collider first touches the wall (~24)

	// Homestead fence AABBs (matches client HomesteadFence).
	fenceThickness          = 7.0
	fenceWorldTile          = 16.0
	fenceTopCollisionOffset = -4.0
	fenceContactInset       = 0.5
)

// FurnitureItem is a placed interior object (world-relative coords).
type FurnitureItem struct {
	Type string  `json:"type"`
	X    float64 `json:"x"`
	Y    float64 `json:"y"`
}

// HouseState is the wire view of a player house.
type HouseState struct {
	ID        int64             `json:"id"`
	OwnerID   int64             `json:"ownerId"`
	OwnerName string            `json:"ownerName"`
	X         float64           `json:"x"`
	Y         float64           `json:"y"`
	Furniture []FurnitureItem   `json:"furniture,omitempty"`
	Crops     []FarmCropState   `json:"crops,omitempty"`
	Animals   []FarmAnimalState `json:"animals,omitempty"`
}

type housePlot struct {
	id             int64
	characterID    int64
	ownerName      string
	centerX        float64
	centerY        float64
	furniture      []FurnitureItem
	crops          []farmTile
	animals        []farmAnimal
	nextAnimalID   int64
	farmDirty      bool
	furnitureDirty bool
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
	plot := &housePlot{
		id: row.ID, characterID: row.CharacterID, ownerName: row.OwnerName,
		centerX: row.CenterX, centerY: row.CenterY, furniture: items,
		nextAnimalID: 1,
	}
	plot.loadFarm(decodeFarm(row.Farm))
	if plot.ensureStarterKitchen() {
		plot.furnitureDirty = true
	}
	return plot
}

func (h *HousingIndex) Snapshot() []HouseState {
	out := make([]HouseState, 0, len(h.byID))
	for _, p := range h.byID {
		out = append(out, p.state())
	}
	return out
}

func (p *housePlot) state() HouseState {
	now := farmNow()
	return HouseState{
		ID: p.id, OwnerID: p.characterID, OwnerName: p.ownerName,
		X: p.centerX, Y: p.centerY, Furniture: append([]FurnitureItem(nil), p.furniture...),
		Crops: p.cropSnapshot(now), Animals: p.animalSnapshot(),
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

// InHouseInterior reports whether (x,y) lies on the interior floor (inside the outer walls).
func InHouseInterior(x, y float64, centerX, centerY float64) bool {
	return x >= interiorTileX(centerX, 1) && x <= interiorTileX(centerX, interiorCols-1) &&
		y >= interiorTileY(centerY, interiorNorthSolidRows+1) && y <= interiorTileY(centerY, interiorRows-1)
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
		if f.Type == "rug" || item.Type == "rug" {
			continue
		}
		if math.Hypot(f.X-item.X, f.Y-item.Y) < FurnitureMinSpacing {
			return "Too close to other furniture."
		}
	}
	if !IsFurnitureType(item.Type) {
		return "Unknown furniture type."
	}
	return ""
}

// furnitureTypes are the pieces a player can place (client HousingDecorateOverlay lists the same).
var furnitureTypes = map[string]bool{
	"bed": true, "table": true, "chair": true, "chest": true, "fireplace": true, "rug": true,
	"kitchen": true, "sofa": true, "armchair": true, "bookshelf": true, "dresser": true,
	"plant": true, "lamp": true, "piano": true,
}

func IsFurnitureType(t string) bool { return furnitureTypes[t] }

// furnitureRemoveRadius is how close a remove click must be to a piece's anchor.
const furnitureRemoveRadius = 24.0

// RemoveFurnitureNear removes the owner's furniture piece closest to (x,y), if one is within
// furnitureRemoveRadius. Returns false when there is nothing there.
func (h *HousingIndex) RemoveFurnitureNear(characterID int64, x, y float64) bool {
	p := h.byCharacter[characterID]
	if p == nil {
		return false
	}
	best := -1
	bestD := furnitureRemoveRadius
	for i, f := range p.furniture {
		if d := math.Hypot(f.X-x, f.Y-y); d <= bestD {
			best, bestD = i, d
		}
	}
	if best < 0 {
		return false
	}
	p.furniture = append(p.furniture[:best], p.furniture[best+1:]...)
	return true
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
// The door plate spans art x 3..19 and y -24..0 from the foot anchor (see
// client FarmRpgHouseSprites.DoorPlate*); the threshold is its bottom-centre.
// World units per art px = DisplayScale (1.55) × 0.5 (client Es).
func HouseDoorPosition(centerX, centerY float64) (float64, float64) {
	const displayScale = 1.55
	const worldPerArtPx = displayScale * 0.5
	const doorCenterArtX = (3.0 + 19.0) / 2
	return centerX + doorCenterArtX*worldPerArtPx, centerY
}

// houseEnterClickRange is how far from the door a click may start an enter. It matches
// the client's Config.InteractRange (72px) with slack for latency. Walking through the
// door still needs to be inside the approach zone.
const houseEnterClickRange = 80.0

// houseExitRange is how far from the interior door an exit request is accepted. The
// client starts the exit as soon as its predicted feet reach the exit zone and then
// stops moving, so the server position is usually a few pixels short of it.
const houseExitRange = 44.0

// doorX and doorY are the door threshold for this plot.
func (p *housePlot) doorX() float64 {
	x, _ := HouseDoorPosition(p.centerX, p.centerY)
	return x
}

func (p *housePlot) doorY() float64 {
	_, y := HouseDoorPosition(p.centerX, p.centerY)
	return y
}

func HouseInteriorSpawn(centerX, centerY float64) (float64, float64) {
	// Just inside the south exit door — player appears at the doorway and walks up into the room.
	dx, dy := HouseInteriorDoorPosition(centerX, centerY)
	return dx, dy - 18
}

// HouseInteriorDoorPosition is the middle of the exit gap in the south wall.
func HouseInteriorDoorPosition(centerX, centerY float64) (float64, float64) {
	gapMid := (interiorTileX(centerX, interiorExitLeft) + interiorTileX(centerX, interiorExitRight+1)) / 2
	return gapMid, interiorTileY(centerY, interiorRows-1) + interiorTile/2
}

// HouseExteriorSpawn puts the player just south of the door approach, on open ground,
// so stepping out never lands them inside the zone that walks them back in.
func HouseExteriorSpawn(centerX, centerY float64) (float64, float64) {
	dx, dy := HouseDoorPosition(centerX, centerY)
	return dx, dy + houseBodyBottom + houseDoorApproachS + 4
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
	return math.Hypot(x-dx, y-dy) <= 19
}

func NearInteriorExit(x, y, centerX, centerY float64) bool {
	dx, dy := HouseInteriorDoorPosition(centerX, centerY)
	if y < dy-14 {
		return false
	}
	return math.Hypot(x-dx, y-dy) <= 26
}

func clampToInterior(x, y, centerX, centerY float64) (float64, float64) {
	// Safety box only; interiorWallRects does the real blocking. The bottom reaches into the
	// exit gap so players can walk out through the door.
	minX := interiorTileX(centerX, 1)
	maxX := interiorTileX(centerX, interiorCols-1)
	minY := interiorTileY(centerY, interiorNorthSolidRows)
	maxY := interiorTileY(centerY, interiorRows) - 2
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
	return feetY - 7*1.5*1.35/2 - ry + 1
}

func overlapsHouseBody(x, y, centerX, centerY float64) bool {
	return overlapsHouseBodyEx(x, y, 6, 8, centerX, centerY, true)
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

	return false
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
	return overlapsHomesteadFenceEntity(x, y, 6, 8, centerX, centerY)
}

func overlapsHomesteadFenceEntity(x, y, rx, ry, centerX, centerY float64) bool {
	left := centerX - PlotHalfW
	top := centerY - PlotHalfH + fenceTopCollisionOffset
	spanX := PlotHalfW * 2
	right := left + spanX
	bottom := centerY + PlotHalfH

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
		if ellipseOverlapsHouseRect(
			x, cy, rx, ry,
			r[0]+fenceContactInset, r[1]-fenceContactInset,
			r[2]+fenceContactInset, r[3]-fenceContactInset,
		) {
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
	if math.Abs(toX-fromX) < 0.0001 && math.Abs(toY-fromY) < 0.0001 {
		return toX, toY
	}
	// Already overlapping a solid (spawned or synced into one): let the player walk
	// out instead of pinning them in place.
	if h.blocksFeet(fromX, fromY) {
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
