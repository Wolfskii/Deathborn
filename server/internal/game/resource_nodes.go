package game

import (
	"fmt"
	"math"
	"sort"

	"github.com/deathborn/server/internal/skills"
	"github.com/deathborn/server/internal/worldmap"
)

// resourceDef says what a gatherable node yields and what a player needs.
type resourceDef struct {
	skill      string
	minLevel   int
	xp         int64
	itemID     string
	depleteSec float64 // time before the node can be gathered again
}

// resourceDefs is keyed by node kind. Rock kinds are defined ahead of time;
// no rock nodes are placed until their positions are chosen.
var resourceDefs = map[string]resourceDef{
	"tree":        {skill: skills.Woodcutting, minLevel: 1, xp: 38, itemID: "logs", depleteSec: 45},
	"rock_copper": {skill: skills.Mining, minLevel: 1, xp: 35, itemID: "copper_ore", depleteSec: 60},
	"rock_iron":   {skill: skills.Mining, minLevel: 15, xp: 60, itemID: "iron_ore", depleteSec: 60},
}

// gatherReachPx is how far a player can stand from a node's anchor and still gather.
const gatherReachPx = PickupRange + 16

// resourceNode is one gatherable tree or rock.
type resourceNode struct {
	id            int64
	kind          string
	x, y          float64
	depleted      bool
	depletedUntil float64 // resource clock time when the node becomes available again
}

// NodeInfo is a resource node as sent to clients.
type NodeInfo struct {
	ID       int64   `json:"id"`
	Kind     string  `json:"kind"`
	X        float64 `json:"x"`
	Y        float64 `json:"y"`
	Depleted bool    `json:"depleted"`
}

// GatherResult describes a successful gather, for the gatherer's client.
type GatherResult struct {
	NodeID    int64
	Item      InventoryItem
	Grant     SkillGrantResult
	Inventory []InventoryItem
}

// buildTreeNodes creates one node per tree the map generates. Ids start at 1
// and follow the map's stable tree order.
func buildTreeNodes(terrain *worldmap.Map) map[int64]*resourceNode {
	nodes := make(map[int64]*resourceNode)
	for i, tp := range terrain.TreePositions() {
		id := int64(i + 1)
		nodes[id] = &resourceNode{id: id, kind: "tree", x: tp.X, y: tp.Y}
	}
	return nodes
}

// NodeList returns every resource node in id order.
func (w *World) NodeList() []NodeInfo {
	w.mu.RLock()
	defer w.mu.RUnlock()
	out := make([]NodeInfo, 0, len(w.nodes))
	for _, n := range w.nodes {
		out = append(out, NodeInfo{ID: n.id, Kind: n.kind, X: n.x, Y: n.y, Depleted: n.depleted})
	}
	sort.Slice(out, func(i, j int) bool { return out[i].ID < out[j].ID })
	return out
}

// ApplyGather checks range, level, depletion and bag space, then grants the
// skill XP and the item and depletes the node. On failure it returns a
// message the player can read.
func (w *World) ApplyGather(playerID, nodeID int64) (GatherResult, string, bool) {
	w.mu.Lock()
	p, ok := w.players[playerID]
	if !ok || p.dead {
		w.mu.Unlock()
		return GatherResult{}, "Cannot gather right now.", false
	}
	if p.insideHouseID != 0 {
		w.mu.Unlock()
		return GatherResult{}, "Leave the house first.", false
	}
	n, ok := w.nodes[nodeID]
	if !ok {
		w.mu.Unlock()
		return GatherResult{}, "Nothing to gather there.", false
	}
	def, ok := resourceDefs[n.kind]
	if !ok {
		w.mu.Unlock()
		return GatherResult{}, "Nothing to gather there.", false
	}
	if n.depleted {
		w.mu.Unlock()
		return GatherResult{}, "Already harvested.", false
	}
	if math.Hypot(p.x-n.x, p.y-n.y) > gatherReachPx {
		w.mu.Unlock()
		return GatherResult{}, "Too far away.", false
	}
	if level := p.skills.Level(def.skill); level < def.minLevel {
		w.mu.Unlock()
		return GatherResult{}, fmt.Sprintf("Your %s level is too low (need %d).", def.skill, def.minLevel), false
	}
	item := InventoryItem{ItemID: def.itemID, Count: 1}
	if !inventoryHasRoom(itemsToSlots(p.inventory), item) {
		w.mu.Unlock()
		return GatherResult{}, "Your bag is full.", false
	}
	n.depleted = true
	n.depletedUntil = w.nodeClock + def.depleteSec
	w.mu.Unlock()

	// These take the lock themselves, so they run after it is released.
	grant, granted := w.GrantSkillXP(playerID, def.skill, def.xp)
	if !granted {
		return GatherResult{}, "Cannot gather right now.", false
	}
	inv := w.AddInventoryItem(playerID, item)
	return GatherResult{NodeID: nodeID, Item: item, Grant: grant, Inventory: inv}, "", true
}

// TickNodes advances the resource clock and returns the ids of nodes that
// became available this tick, so clients can be told.
func (w *World) TickNodes(dt float64) []int64 {
	w.mu.Lock()
	defer w.mu.Unlock()
	w.nodeClock += dt
	var back []int64
	for id, n := range w.nodes {
		if n.depleted && n.depletedUntil <= w.nodeClock {
			n.depleted = false
			back = append(back, id)
		}
	}
	sort.Slice(back, func(i, j int) bool { return back[i] < back[j] })
	return back
}
