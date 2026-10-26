package net

import (
	"fmt"

	"github.com/deathborn/server/internal/game"
)

// deathReason returns the reason text and killer name. killerID > 0 is a
// player, killerID < 0 is a boss or mob, and 0 means no known killer.
func deathReason(killerID int64, killerName string) (reason, killer string) {
	switch {
	case killerID > 0 && killerName != "":
		return "Slain by " + killerName, killerName
	case killerID < 0 && killerName != "":
		return "Killed by " + killerName, killerName
	default:
		return "Died in the wild", ""
	}
}

// deathSummary is the one-line life summary shown on the death panel.
func deathSummary(totalLevel int, survivalSeconds int64, pvpKills int) string {
	kills := "kills"
	if pvpKills == 1 {
		kills = "kill"
	}
	return fmt.Sprintf("Total level %d, %s survived, %d %s",
		totalLevel, formatSurvival(survivalSeconds), pvpKills, kills)
}

// formatSurvival renders a duration as "12 min" or "2h 5m".
func formatSurvival(seconds int64) string {
	minutes := seconds / 60
	if minutes < 60 {
		return fmt.Sprintf("%d min", minutes)
	}
	return fmt.Sprintf("%dh %dm", minutes/60, minutes%60)
}

// inventoryWealth is a placeholder until gold exists: the total item count.
func inventoryWealth(items []game.InventoryItem) int64 {
	var total int64
	for _, it := range items {
		total += int64(it.Count)
	}
	return total
}
