package game

import "github.com/deathborn/server/internal/skills"

// SkillGrantResult is returned when XP is awarded.
type SkillGrantResult struct {
	SkillID   string
	Xp        int64
	Level     int
	LeveledUp bool
}

// GrantSkillXP adds XP to one skill and refreshes HP max from hitpoints level.
func (w *World) GrantSkillXP(playerID int64, skill string, amount int64) (SkillGrantResult, bool) {
	if amount <= 0 || skill == "" {
		return SkillGrantResult{}, false
	}
	w.mu.Lock()
	defer w.mu.Unlock()

	p, ok := w.players[playerID]
	if !ok || p.dead || p.skills == nil {
		return SkillGrantResult{}, false
	}

	newXp, level, leveledUp := p.skills.Grant(skill, amount)
	p.totalXp = p.skills.TotalXP()
	w.applyHitpointsMax(p)

	return SkillGrantResult{
		SkillID:   skill,
		Xp:        newXp,
		Level:     level,
		LeveledUp: leveledUp,
	}, true
}

func (w *World) applyHitpointsMax(p *player) {
	if p.skills == nil {
		return
	}
	hpLevel := p.skills.Level(skills.Hitpoints)
	newMax := skills.HitpointsMax(hpLevel)
	if newMax == p.hpMax {
		return
	}
	gain := newMax - p.hpMax
	p.hpMax = newMax
	if gain > 0 {
		p.hp += gain
	}
	if p.hp > p.hpMax {
		p.hp = p.hpMax
	}
}

// PlayerSkillsSnapshot returns a copy of a player's skill XP and total.
func (w *World) PlayerSkillsSnapshot(playerID int64) (skills.Set, int64, bool) {
	w.mu.RLock()
	defer w.mu.RUnlock()
	p, ok := w.players[playerID]
	if !ok || p.skills == nil {
		return nil, 0, false
	}
	copy := make(skills.Set, len(p.skills))
	for k, v := range p.skills {
		copy[k] = v
	}
	return copy, p.totalXp, true
}

// SkillsForSave returns skill data for DB persistence.
func (w *World) SkillsForSave(playerID int64) (skills.Set, int64, bool) {
	return w.PlayerSkillsSnapshot(playerID)
}

// CanInteractSkill checks gather cooldown.
func (w *World) CanInteractSkill(playerID int64, now float64, cooldownSec float64) bool {
	w.mu.Lock()
	defer w.mu.Unlock()
	p, ok := w.players[playerID]
	if !ok {
		return false
	}
	if now-p.lastSkillInteract < cooldownSec {
		return false
	}
	p.lastSkillInteract = now
	return true
}
