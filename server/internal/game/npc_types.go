package game

// NpcDisposition controls combat/interaction rules.
type NpcDisposition string

const (
	NpcHostile  NpcDisposition = "hostile"
	NpcNeutral  NpcDisposition = "neutral"
	NpcFriendly NpcDisposition = "friendly"
)

// NpcCategory groups NPCs for spawn rules and rendering.
type NpcCategory string

const (
	NpcCategoryBoss       NpcCategory = "boss"
	NpcCategoryMonster    NpcCategory = "monster"
	NpcCategoryWildAnimal NpcCategory = "wild_animal"
	NpcCategoryQuestNpc   NpcCategory = "quest_npc"
	NpcCategoryVendor     NpcCategory = "vendor"
	NpcCategoryGuard      NpcCategory = "guard"
)

func (c NpcCategory) blocksTowns() bool {
	return c == NpcCategoryMonster || c == NpcCategoryWildAnimal || c == NpcCategoryBoss
}

type npcDef struct {
	id          string
	name        string
	category    NpcCategory
	disposition NpcDisposition
	spriteID    string
	hpMax       float64
	speed       float64
	radius      float64
	wander      bool
	leash       float64
}

var npcDefs = map[string]npcDef{
	"forest_skeleton": {
		id: "forest_skeleton", name: "Skeleton", category: NpcCategoryMonster,
		disposition: NpcHostile, spriteID: "skeleton", hpMax: 40, speed: 28,
		radius: 14, wander: true, leash: 96,
	},
	"forest_slime": {
		id: "forest_slime", name: "Slime", category: NpcCategoryMonster,
		disposition: NpcHostile, spriteID: "slime", hpMax: 25, speed: 22,
		radius: 12, wander: true, leash: 72,
	},
	"forest_orc": {
		id: "forest_orc", name: "Orc", category: NpcCategoryMonster,
		disposition: NpcHostile, spriteID: "orc", hpMax: 55, speed: 32,
		radius: 16, wander: true, leash: 110,
	},
	"wild_bat": {
		id: "wild_bat", name: "Bat", category: NpcCategoryWildAnimal,
		disposition: NpcHostile, spriteID: "bat", hpMax: 18, speed: 38,
		radius: 10, wander: true, leash: 80,
	},
	"town_guard": {
		id: "town_guard", name: "Town Guard", category: NpcCategoryGuard,
		disposition: NpcFriendly, spriteID: "soldier", hpMax: 100, speed: 0,
		radius: 14, wander: false,
	},
	"town_priest": {
		id: "town_priest", name: "Priest", category: NpcCategoryQuestNpc,
		disposition: NpcFriendly, spriteID: "priest", hpMax: 50, speed: 0,
		radius: 14, wander: false,
	},
	"town_wizard": {
		id: "town_wizard", name: "Wizard", category: NpcCategoryQuestNpc,
		disposition: NpcFriendly, spriteID: "wizard", hpMax: 50, speed: 0,
		radius: 14, wander: false,
	},
}

func lookupNpcDef(id string) (npcDef, bool) {
	d, ok := npcDefs[id]
	return d, ok
}
