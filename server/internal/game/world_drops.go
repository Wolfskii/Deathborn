package game

import (
	"math"
	"sync"

	"github.com/deathborn/server/internal/db"
)

// WorldItemDropState is the wire view of ground loot.
type WorldItemDropState struct {
	ID      int64   `json:"id"`
	ItemID  string  `json:"itemId"`
	HouseID int64   `json:"houseId,omitempty"`
	Count   int     `json:"count"`
	X       float64 `json:"x"`
	Y       float64 `json:"y"`
}

type worldDrop struct {
	id      int64
	itemID  string
	houseID int64
	count   int
	x, y    float64
}

// WorldDropIndex tracks loot piles on the ground.
type WorldDropIndex struct {
	mu    sync.RWMutex
	byID  map[int64]*worldDrop
	nextID int64
}

func NewWorldDropIndex() *WorldDropIndex {
	return &WorldDropIndex{byID: make(map[int64]*worldDrop), nextID: 1}
}

func (d *WorldDropIndex) LoadFromDB(rows []db.WorldItemDrop) {
	d.mu.Lock()
	defer d.mu.Unlock()
	d.byID = make(map[int64]*worldDrop)
	var maxID int64
	for _, row := range rows {
		d.byID[row.ID] = &worldDrop{
			id: row.ID, itemID: row.ItemID, houseID: row.HouseID,
			count: row.Count, x: row.X, y: row.Y,
		}
		if row.ID > maxID {
			maxID = row.ID
		}
	}
	d.nextID = maxID + 1
}

func (d *WorldDropIndex) Snapshot() []WorldItemDropState {
	d.mu.RLock()
	defer d.mu.RUnlock()
	out := make([]WorldItemDropState, 0, len(d.byID))
	for _, drop := range d.byID {
		out = append(out, drop.state())
	}
	return out
}

func (drop *worldDrop) state() WorldItemDropState {
	return WorldItemDropState{
		ID: drop.id, ItemID: drop.itemID, HouseID: drop.houseID,
		Count: drop.count, X: drop.x, Y: drop.y,
	}
}

func (d *WorldDropIndex) Add(drop *worldDrop) {
	d.mu.Lock()
	defer d.mu.Unlock()
	if drop.id <= 0 {
		drop.id = d.nextID
		d.nextID++
	}
	d.byID[drop.id] = drop
	if drop.id >= d.nextID {
		d.nextID = drop.id + 1
	}
}

func (d *WorldDropIndex) Remove(id int64) *worldDrop {
	d.mu.Lock()
	defer d.mu.Unlock()
	drop := d.byID[id]
	delete(d.byID, id)
	return drop
}

func (d *WorldDropIndex) Get(id int64) *worldDrop {
	d.mu.RLock()
	defer d.mu.RUnlock()
	return d.byID[id]
}

func (d *WorldDropIndex) At(x, y float64) *worldDrop {
	d.mu.RLock()
	defer d.mu.RUnlock()
	for _, drop := range d.byID {
		if math.Hypot(drop.x-x, drop.y-y) <= PickupRange {
			return drop
		}
	}
	return nil
}

func (w *World) LoadWorldDrops(rows []db.WorldItemDrop) {
	if w.drops == nil {
		w.drops = NewWorldDropIndex()
	}
	w.drops.LoadFromDB(rows)
}

func (w *World) DropSnapshot() []WorldItemDropState {
	if w.drops == nil {
		return nil
	}
	return w.drops.Snapshot()
}

func (w *World) RegisterDrop(row db.WorldItemDrop) WorldItemDropState {
	if w.drops == nil {
		w.drops = NewWorldDropIndex()
	}
	drop := &worldDrop{
		id: row.ID, itemID: row.ItemID, houseID: row.HouseID,
		count: row.Count, x: row.X, y: row.Y,
	}
	w.drops.Add(drop)
	return drop.state()
}

func (w *World) RemoveDrop(id int64) (WorldItemDropState, bool) {
	if w.drops == nil {
		return WorldItemDropState{}, false
	}
	drop := w.drops.Remove(id)
	if drop == nil {
		return WorldItemDropState{}, false
	}
	return drop.state(), true
}

func (w *World) CanPickupDrop(characterID, dropID int64) (WorldItemDropState, string, bool) {
	w.mu.RLock()
	defer w.mu.RUnlock()
	p, ok := w.players[characterID]
	if !ok || p.dead {
		return WorldItemDropState{}, "Cannot pick that up.", false
	}
	if w.drops == nil {
		return WorldItemDropState{}, "Nothing to pick up.", false
	}
	drop := w.drops.Get(dropID)
	if drop == nil {
		return WorldItemDropState{}, "That item is gone.", false
	}
	if math.Hypot(p.x-drop.x, p.y-drop.y) > PickupRange {
		return WorldItemDropState{}, "Move closer to pick that up.", false
	}
	if drop.itemID == db.ItemHouseKey {
		if w.housing != nil && w.housing.ByCharacter(characterID) != nil {
			return WorldItemDropState{}, "You already have a homestead.", false
		}
		if playerHasHouseKey(p.inventory) {
			return WorldItemDropState{}, "You already carry a homestead key.", false
		}
	}
	return drop.state(), "", true
}
