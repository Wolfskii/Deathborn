package game

import "github.com/deathborn/server/internal/db"

const CosmeticSlotHead = "head"

var cosmeticItems = map[string]string{
	"santa_hat":          CosmeticSlotHead,
	"party_hat":          CosmeticSlotHead,
	"jester_cap":         CosmeticSlotHead,
	"bucket_helmet":      CosmeticSlotHead,
	"pirate_hat":         CosmeticSlotHead,
	"propeller_hat":      CosmeticSlotHead,
	"bunny_ears":         CosmeticSlotHead,
	"top_hat":            CosmeticSlotHead,
	"traffic_cone":       CosmeticSlotHead,
	"beer_helm":          CosmeticSlotHead,
	"wizard_hat_torn":    CosmeticSlotHead,
	"crown_of_bones":     CosmeticSlotHead,
	"rubber_chicken_hat": CosmeticSlotHead,
	"grim_hood":          CosmeticSlotHead,
	"gold_helm_rusty":    CosmeticSlotHead,
	"fedora_of_shame":    CosmeticSlotHead,
	"clown_nose_glasses": CosmeticSlotHead,
	"severed_elf_hat":    CosmeticSlotHead,
}

func IsCosmeticItem(itemID string) bool {
	_, ok := cosmeticItems[itemID]
	return ok
}

func CosmeticSlotForItem(itemID string) string {
	return cosmeticItems[itemID]
}

func (w *World) HeadCosmetic(characterID int64) string {
	w.mu.RLock()
	defer w.mu.RUnlock()
	p := w.players[characterID]
	if p == nil {
		return ""
	}
	return p.headCosmetic
}

func (w *World) SetPlayerCosmetics(characterID int64, cosmetics db.EquippedCosmetics) {
	w.mu.Lock()
	defer w.mu.Unlock()
	p := w.players[characterID]
	if p == nil {
		return
	}
	p.headCosmetic = cosmetics[CosmeticSlotHead]
	syncPlayerCosmeticInventory(p)
}

func (w *World) EquipCosmeticFromSlot(characterID int64, slot int) (string, string, bool) {
	w.mu.Lock()
	defer w.mu.Unlock()
	p := w.players[characterID]
	if p == nil || p.dead {
		return "", "Cannot equip right now.", false
	}
	if slot < 0 || slot >= InventorySlotCount {
		return "", "Invalid slot.", false
	}
	slots := itemsToSlots(p.inventory)
	item := slots[slot]
	if item.ItemID == "" || item.Count <= 0 {
		return "", "That slot is empty.", false
	}
	cosmeticSlot := CosmeticSlotForItem(item.ItemID)
	if cosmeticSlot == "" {
		return "", "That item is not wearable.", false
	}
	if cosmeticSlot == CosmeticSlotHead && p.headCosmetic == item.ItemID {
		p.headCosmetic = ""
		return "", "", true
	}
	if cosmeticSlot == CosmeticSlotHead {
		p.headCosmetic = item.ItemID
	}
	return p.headCosmetic, "", true
}

func syncPlayerCosmeticInventory(p *player) {
	if p.headCosmetic == "" {
		return
	}
	if !inventoryContainsItem(p.inventory, p.headCosmetic) {
		p.headCosmetic = ""
	}
}

func inventoryContainsItem(items []InventoryItem, itemID string) bool {
	for _, it := range items {
		if it.ItemID == itemID && it.Count > 0 {
			return true
		}
	}
	return false
}

func playerCosmeticsToDB(p *player) db.EquippedCosmetics {
	out := db.EquippedCosmetics{}
	if p.headCosmetic != "" {
		out[CosmeticSlotHead] = p.headCosmetic
	}
	return out
}

func CosmeticsFromDB(c db.EquippedCosmetics) string {
	return cosmeticsFromDB(c)
}

func cosmeticsFromDB(c db.EquippedCosmetics) string {
	if c == nil {
		return ""
	}
	return c[CosmeticSlotHead]
}
