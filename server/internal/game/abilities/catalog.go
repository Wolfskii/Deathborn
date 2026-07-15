package abilities

import (
	_ "embed"
	"encoding/json"
	"fmt"
)

//go:embed abilities.json
var embeddedJSON []byte

// Catalog is the loaded ability registry (id → def).
type Catalog struct {
	byID map[string]Def
}

var defaultCatalog = mustLoad()

func mustLoad() *Catalog {
	c, err := LoadJSON(embeddedJSON)
	if err != nil {
		panic(fmt.Sprintf("abilities: load embedded catalog: %v", err))
	}
	return c
}

// Default returns the embedded ability catalog.
func Default() *Catalog { return defaultCatalog }

// LoadJSON parses an abilities.json payload.
func LoadJSON(data []byte) (*Catalog, error) {
	var f file
	if err := json.Unmarshal(data, &f); err != nil {
		return nil, err
	}
	if f.Version < 1 {
		return nil, fmt.Errorf("abilities: unsupported version %d", f.Version)
	}
	c := &Catalog{byID: make(map[string]Def, len(f.Abilities))}
	for _, def := range f.Abilities {
		if def.ID == "" {
			return nil, fmt.Errorf("abilities: missing id")
		}
		if _, dup := c.byID[def.ID]; dup {
			return nil, fmt.Errorf("abilities: duplicate id %q", def.ID)
		}
		c.byID[def.ID] = def
	}
	return c, nil
}

// Get returns one ability definition.
func (c *Catalog) Get(id string) (Def, bool) {
	def, ok := c.byID[id]
	return def, ok
}

// Damage returns authoritative damage for a damaging ability id.
func (c *Catalog) Damage(id string) int {
	if def, ok := c.byID[id]; ok {
		return def.Damage()
	}
	return 0
}

// HitRange returns max hit validation distance for an ability id.
func (c *Catalog) HitRange(id string) float64 {
	if def, ok := c.byID[id]; ok {
		return def.HitRange()
	}
	return 0
}

// Heal returns instant heal amount for a heal ability id.
func (c *Catalog) Heal(id string) int {
	if def, ok := c.byID[id]; ok {
		return def.Heal()
	}
	return 0
}

// HoT returns heal-over-time spec when defined (e.g. bandage).
func (c *Catalog) HoT(id string) (*HoTSpec, bool) {
	def, ok := c.byID[id]
	if !ok || def.HoT == nil {
		return nil, false
	}
	return def.HoT, true
}
