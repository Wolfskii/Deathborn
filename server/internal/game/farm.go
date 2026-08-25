package game

import (
	"math"
	"time"
)

const (
	FarmTileSize       = 16.0
	FarmMaxTiles       = 24
	FarmMaxAnimals     = 4
	FarmActionRange    = 40.0
	FarmWaterDuration  = 90.0
	FarmProductReadyIn = 75.0
	FarmFeedDuration   = 180.0
)

// FarmCropState is the wire view of one tilled homestead tile.
type FarmCropState struct {
	Tx       int    `json:"tx"`
	Ty       int    `json:"ty"`
	Crop     string `json:"crop,omitempty"`
	Stage    int    `json:"stage,omitempty"`
	Watered  bool   `json:"watered"`
	Ready    bool   `json:"ready,omitempty"`
	MaxStage int    `json:"maxStage,omitempty"`
}

// FarmAnimalState is the wire view of a homestead animal.
type FarmAnimalState struct {
	ID           int64   `json:"id"`
	Type         string  `json:"type"`
	X            float64 `json:"x"`
	Y            float64 `json:"y"`
	ProductReady bool    `json:"productReady,omitempty"`
}

type farmTile struct {
	tx           int
	ty           int
	crop         string
	stage        int
	growAccum    float64
	wateredUntil float64
}

type farmAnimal struct {
	id           int64
	kind         string
	x, y         float64
	productReady bool
	productAt    float64
	feedUntil    float64
}

type cropDef struct {
	id          string
	name        string
	seedItem    string
	produceItem string
	stages      int
	stageSec    float64
	regrowStage int // -1 = pull up the plant; else reset to this stage
	xp          int64
	yield       int
}

type animalDef struct {
	id          string
	name        string
	item        string
	product     string
	productName string
	xp          int64
}

var cropDefs = map[string]cropDef{
	"parsnip":    {id: "parsnip", name: "Parsnip", seedItem: "parsnip_seeds", produceItem: "parsnip", stages: 5, stageSec: 22, regrowStage: -1, xp: 32, yield: 1},
	"carrot":     {id: "carrot", name: "Carrot", seedItem: "carrot_seeds", produceItem: "carrot", stages: 6, stageSec: 24, regrowStage: -1, xp: 36, yield: 1},
	"potato":     {id: "potato", name: "Potato", seedItem: "potato_seeds", produceItem: "potato", stages: 6, stageSec: 24, regrowStage: -1, xp: 38, yield: 2},
	"strawberry": {id: "strawberry", name: "Strawberry", seedItem: "strawberry_seeds", produceItem: "strawberry", stages: 6, stageSec: 26, regrowStage: 3, xp: 42, yield: 2},
	"tomato":     {id: "tomato", name: "Tomato", seedItem: "tomato_seeds", produceItem: "tomato", stages: 7, stageSec: 26, regrowStage: 3, xp: 44, yield: 2},
	"wheat":      {id: "wheat", name: "Wheat", seedItem: "wheat_seeds", produceItem: "wheat", stages: 6, stageSec: 28, regrowStage: -1, xp: 40, yield: 1},
	"melon":      {id: "melon", name: "Melon", seedItem: "melon_seeds", produceItem: "melon", stages: 6, stageSec: 32, regrowStage: -1, xp: 52, yield: 1},
	"pumpkin":    {id: "pumpkin", name: "Pumpkin", seedItem: "pumpkin_seeds", produceItem: "pumpkin", stages: 5, stageSec: 34, regrowStage: -1, xp: 55, yield: 1},
	"corn":       {id: "corn", name: "Corn", seedItem: "corn_seeds", produceItem: "corn", stages: 8, stageSec: 28, regrowStage: 4, xp: 48, yield: 2},
	"beetroot":   {id: "beetroot", name: "Beetroot", seedItem: "beetroot_seeds", produceItem: "beetroot", stages: 6, stageSec: 26, regrowStage: -1, xp: 40, yield: 1},
}

var animalDefs = map[string]animalDef{
	"chicken": {id: "chicken", name: "Chicken", item: "chicken", product: "chicken_egg", productName: "Chicken Egg", xp: 22},
	"cow":     {id: "cow", name: "Cow", item: "cow", product: "milk", productName: "Milk", xp: 36},
	"sheep":   {id: "sheep", name: "Sheep", item: "sheep", product: "wool", productName: "Wool", xp: 30},
	"pig":     {id: "pig", name: "Pig", item: "pig", product: "", productName: "", xp: 18},
}

var seedToCrop = func() map[string]string {
	m := make(map[string]string, len(cropDefs))
	for id, d := range cropDefs {
		m[d.seedItem] = id
	}
	return m
}()

func cropDefBySeed(itemID string) (cropDef, bool) {
	id, ok := seedToCrop[itemID]
	if !ok {
		return cropDef{}, false
	}
	d, ok := cropDefs[id]
	return d, ok
}

func animalDefByItem(itemID string) (animalDef, bool) {
	d, ok := animalDefs[itemID]
	return d, ok
}

func farmNow() float64 {
	return float64(time.Now().UnixMilli()) / 1000
}

func worldTile(x, y float64) (int, int) {
	return int(math.Floor(x / FarmTileSize)), int(math.Floor(y / FarmTileSize))
}

func tileCenter(tx, ty int) (float64, float64) {
	return (float64(tx) + 0.5) * FarmTileSize, (float64(ty) + 0.5) * FarmTileSize
}

func (t farmTile) wateredAt(now float64) bool {
	return now < t.wateredUntil
}

func (t farmTile) ready() bool {
	if t.crop == "" {
		return false
	}
	d, ok := cropDefs[t.crop]
	if !ok {
		return false
	}
	return t.stage >= d.stages-1
}

func (t farmTile) wire(now float64) FarmCropState {
	max := 0
	if d, ok := cropDefs[t.crop]; ok {
		max = d.stages - 1
	}
	return FarmCropState{
		Tx: t.tx, Ty: t.ty, Crop: t.crop, Stage: t.stage,
		Watered: t.wateredAt(now), Ready: t.ready(), MaxStage: max,
	}
}

func (a farmAnimal) wire() FarmAnimalState {
	return FarmAnimalState{ID: a.id, Type: a.kind, X: a.x, Y: a.y, ProductReady: a.productReady}
}

func (p *housePlot) findCrop(tx, ty int) int {
	for i := range p.crops {
		if p.crops[i].tx == tx && p.crops[i].ty == ty {
			return i
		}
	}
	return -1
}

func (p *housePlot) findAnimal(id int64) int {
	for i := range p.animals {
		if p.animals[i].id == id {
			return i
		}
	}
	return -1
}

func (p *housePlot) nearestAnimal(x, y, maxDist float64) int {
	best := -1
	bestD := maxDist * maxDist
	for i, a := range p.animals {
		dx := a.x - x
		dy := a.y - y
		d := dx*dx + dy*dy
		if d <= bestD {
			bestD = d
			best = i
		}
	}
	return best
}

func (p *housePlot) inPlot(x, y float64) bool {
	return x >= p.centerX-PlotHalfW && x <= p.centerX+PlotHalfW &&
		y >= p.centerY-PlotHalfH && y <= p.centerY+PlotHalfH
}

func (p *housePlot) tileFarmable(tx, ty int) bool {
	cx, cy := tileCenter(tx, ty)
	if !p.inPlot(cx, cy) {
		return false
	}
	// Keep the cottage footprint and door approach clear.
	if overlapsHouseBody(cx, cy, p.centerX, p.centerY) {
		return false
	}
	if inDoorApproach(cx, cy, p.centerX, p.centerY) {
		return false
	}
	return true
}

func (p *housePlot) tickFarm(dt, now float64) bool {
	changed := false
	for i := range p.crops {
		t := &p.crops[i]
		wasWet := t.wateredAt(now - dt)
		if t.crop == "" {
			if wasWet && !t.wateredAt(now) {
				changed = true
			}
			continue
		}
		d, ok := cropDefs[t.crop]
		if !ok {
			continue
		}
		if t.stage >= d.stages-1 {
			if wasWet && !t.wateredAt(now) {
				changed = true
			}
			continue
		}
		if !t.wateredAt(now) {
			if wasWet {
				changed = true
			}
			continue
		}
		t.growAccum += dt
		for t.stage < d.stages-1 && t.growAccum >= d.stageSec {
			t.growAccum -= d.stageSec
			t.stage++
			changed = true
		}
	}

	for i := range p.animals {
		a := &p.animals[i]
		d, ok := animalDefs[a.kind]
		if !ok || d.product == "" {
			continue
		}
		if a.productReady {
			continue
		}
		if now >= a.productAt {
			a.productReady = true
			changed = true
		}
	}
	return changed
}

func (p *housePlot) cropSnapshot(now float64) []FarmCropState {
	if len(p.crops) == 0 {
		return nil
	}
	out := make([]FarmCropState, len(p.crops))
	for i, t := range p.crops {
		out[i] = t.wire(now)
	}
	return out
}

func (p *housePlot) animalSnapshot() []FarmAnimalState {
	if len(p.animals) == 0 {
		return nil
	}
	out := make([]FarmAnimalState, len(p.animals))
	for i, a := range p.animals {
		out[i] = a.wire()
	}
	return out
}

// FarmPersist is the JSON blob stored on the house row.
type FarmPersist struct {
	Crops        []farmTilePersist   `json:"crops,omitempty"`
	Animals      []farmAnimalPersist `json:"animals,omitempty"`
	NextAnimalID int64               `json:"nextAnimalId,omitempty"`
}

type farmTilePersist struct {
	Tx           int     `json:"tx"`
	Ty           int     `json:"ty"`
	Crop         string  `json:"crop,omitempty"`
	Stage        int     `json:"stage,omitempty"`
	GrowAccum    float64 `json:"growAccum,omitempty"`
	WateredUntil float64 `json:"wateredUntil,omitempty"`
}

type farmAnimalPersist struct {
	ID           int64   `json:"id"`
	Type         string  `json:"type"`
	X            float64 `json:"x"`
	Y            float64 `json:"y"`
	ProductReady bool    `json:"productReady,omitempty"`
	ProductAt    float64 `json:"productAt,omitempty"`
	FeedUntil    float64 `json:"feedUntil,omitempty"`
}

func (p *housePlot) farmPersist() FarmPersist {
	crops := make([]farmTilePersist, len(p.crops))
	for i, t := range p.crops {
		crops[i] = farmTilePersist{
			Tx: t.tx, Ty: t.ty, Crop: t.crop, Stage: t.stage,
			GrowAccum: t.growAccum, WateredUntil: t.wateredUntil,
		}
	}
	animals := make([]farmAnimalPersist, len(p.animals))
	for i, a := range p.animals {
		animals[i] = farmAnimalPersist{
			ID: a.id, Type: a.kind, X: a.x, Y: a.y,
			ProductReady: a.productReady, ProductAt: a.productAt, FeedUntil: a.feedUntil,
		}
	}
	return FarmPersist{Crops: crops, Animals: animals, NextAnimalID: p.nextAnimalID}
}

func (p *housePlot) loadFarm(data FarmPersist) {
	p.crops = p.crops[:0]
	for _, t := range data.Crops {
		p.crops = append(p.crops, farmTile{
			tx: t.Tx, ty: t.Ty, crop: t.Crop, stage: t.Stage,
			growAccum: t.GrowAccum, wateredUntil: t.WateredUntil,
		})
	}
	p.animals = p.animals[:0]
	maxID := data.NextAnimalID
	now := farmNow()
	for _, a := range data.Animals {
		if a.ID > maxID {
			maxID = a.ID
		}
		productAt := a.ProductAt
		if productAt <= 0 {
			productAt = now + FarmProductReadyIn
		}
		p.animals = append(p.animals, farmAnimal{
			id: a.ID, kind: a.Type, x: a.X, y: a.Y,
			productReady: a.ProductReady, productAt: productAt, feedUntil: a.FeedUntil,
		})
	}
	if maxID < 1 {
		maxID = 1
	}
	p.nextAnimalID = maxID
}

func IsFarmTool(itemID string) bool {
	return itemID == "hoe" || itemID == "watering_can"
}

func IsFarmSeed(itemID string) bool {
	_, ok := seedToCrop[itemID]
	return ok
}

func IsFarmAnimalItem(itemID string) bool {
	_, ok := animalDefs[itemID]
	return ok
}

func FarmItemMaxStack(itemID string) int {
	if IsFarmTool(itemID) || IsFarmAnimalItem(itemID) {
		return 1
	}
	if IsFarmSeed(itemID) || itemID == "animal_feed" {
		return 40
	}
	for _, d := range cropDefs {
		if d.produceItem == itemID {
			return 40
		}
	}
	for _, d := range animalDefs {
		if d.product == itemID {
			return 40
		}
	}
	return 0
}

func FarmStarterKit() []InventoryItem {
	return []InventoryItem{
		{ItemID: "hoe", Count: 1},
		{ItemID: "watering_can", Count: 1},
		{ItemID: "parsnip_seeds", Count: 15},
		{ItemID: "potato_seeds", Count: 10},
		{ItemID: "carrot_seeds", Count: 10},
		{ItemID: "chicken", Count: 1},
		{ItemID: "cow", Count: 1},
		{ItemID: "sheep", Count: 1},
		{ItemID: "animal_feed", Count: 12},
	}
}
