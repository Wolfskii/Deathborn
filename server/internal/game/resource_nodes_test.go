package game

import (
	"strings"
	"testing"
)

// newGatherWorld builds a world with one tree and one iron rock at (100,100)
// and a player standing next to them.
func newGatherWorld(t *testing.T) *World {
	t.Helper()
	w := &World{
		players: make(map[int64]*player),
		nodes: map[int64]*resourceNode{
			1: {id: 1, kind: "tree", x: 100, y: 100},
			2: {id: 2, kind: "rock_iron", x: 100, y: 100},
		},
	}
	w.AddPlayer(1, "Lumber", "human", PlayerAppearance{}, 110, 100, nil, 0, nil, "", PlayerVitals{})
	return w
}

func TestGatherGrantsXPAndItemAndDepletesNode(t *testing.T) {
	w := newGatherWorld(t)
	res, msg, ok := w.ApplyGather(1, 1)
	if !ok {
		t.Fatalf("gather failed: %s", msg)
	}
	if res.Item.ItemID != "logs" || res.Item.Count != 1 {
		t.Fatalf("item = %+v, want 1 logs", res.Item)
	}
	if res.Grant.SkillID != "woodcutting" || res.Grant.Xp < 38 {
		t.Fatalf("grant = %+v, want woodcutting xp >= 38", res.Grant)
	}
	if !w.nodes[1].depleted {
		t.Fatal("node should be depleted after gathering")
	}
	if len(res.Inventory) != 1 || res.Inventory[0].ItemID != "logs" {
		t.Fatalf("inventory = %+v, want the logs", res.Inventory)
	}
}

func TestGatherRejectsDepletedNode(t *testing.T) {
	w := newGatherWorld(t)
	if _, _, ok := w.ApplyGather(1, 1); !ok {
		t.Fatal("first gather should succeed")
	}
	if _, msg, ok := w.ApplyGather(1, 1); ok || msg != "Already harvested." {
		t.Fatalf("second gather: ok=%v msg=%q, want Already harvested", ok, msg)
	}
}

func TestGatherRejectsOutOfReach(t *testing.T) {
	w := newGatherWorld(t)
	w.players[1].x, w.players[1].y = 500, 500
	if _, msg, ok := w.ApplyGather(1, 1); ok || msg != "Too far away." {
		t.Fatalf("far gather: ok=%v msg=%q, want Too far away", ok, msg)
	}
}

func TestGatherRejectsLowLevel(t *testing.T) {
	w := newGatherWorld(t)
	_, msg, ok := w.ApplyGather(1, 2)
	if ok || !strings.Contains(msg, "too low") {
		t.Fatalf("iron at level 1: ok=%v msg=%q, want level too low", ok, msg)
	}
}

func TestGatherRejectsFullBag(t *testing.T) {
	w := newGatherWorld(t)
	full := make([]InventoryItem, 0, InventorySlotCount)
	for i := 0; i < InventorySlotCount; i++ {
		full = append(full, InventoryItem{Slot: i, ItemID: "junk_" + string(rune('a'+i)), Count: 1})
	}
	w.players[1].inventory = full
	if _, msg, ok := w.ApplyGather(1, 1); ok || msg != "Your bag is full." {
		t.Fatalf("full bag: ok=%v msg=%q, want bag full", ok, msg)
	}
	if w.nodes[1].depleted {
		t.Fatal("a failed gather must not deplete the node")
	}
}

func TestGatherUnknownNode(t *testing.T) {
	w := newGatherWorld(t)
	if _, _, ok := w.ApplyGather(1, 999); ok {
		t.Fatal("unknown node must be rejected")
	}
}

func TestTickNodesReturnsNodeWhenItRespawns(t *testing.T) {
	w := newGatherWorld(t)
	if _, _, ok := w.ApplyGather(1, 1); !ok {
		t.Fatal("gather failed")
	}
	if back := w.TickNodes(44); len(back) != 0 {
		t.Fatalf("node came back too early: %v", back)
	}
	back := w.TickNodes(2)
	if len(back) != 1 || back[0] != 1 {
		t.Fatalf("node should come back after 45s, got %v", back)
	}
	if w.nodes[1].depleted {
		t.Fatal("node should be available again")
	}
}

func TestNodeListReportsDepletedState(t *testing.T) {
	w := newGatherWorld(t)
	if _, _, ok := w.ApplyGather(1, 1); !ok {
		t.Fatal("gather failed")
	}
	list := w.NodeList()
	if len(list) != 2 || list[0].ID != 1 || list[1].ID != 2 {
		t.Fatalf("node list not in id order: %+v", list)
	}
	if !list[0].Depleted || list[1].Depleted {
		t.Fatalf("depleted flags wrong: %+v", list)
	}
}
