package game

import "math"

const (
	BuffBattleShout = "battle_shout"
	BuffIronSkin    = "iron_skin"
	BuffHunterMark  = "hunter_mark"

	BattleShoutDuration = 8.0 * 60.0  // 8 minutes
	IronSkinDuration    = 10.0 * 60.0 // 10 minutes
	HunterMarkDuration  = 15.0 * 60.0 // 15 minutes

	BattleShoutDamageBonus = 0.25
	IronSkinDamageReduce   = 0.30
)

type playerBuff struct {
	id         string
	remain     float64
	duration   float64
	markTarget int64
}

// BuffEvent is emitted when a buff is applied, restored, or expires.
// Remaining is time left; Duration is the full length (for HUD progress).
// Expire events use Remaining == 0 and Duration == 0.
type BuffEvent struct {
	PlayerID     int64
	BuffID       string
	Remaining    float64
	Duration     float64
	MarkTargetID int64
}

// SavedBuff is a paused buff snapshot for DB persistence.
type SavedBuff struct {
	ID           string
	Remain       float64
	Duration     float64
	MarkTargetID int64
}

func (w *World) ApplyPlayerBuff(playerID int64, buffID string, markTarget int64) (BuffEvent, bool) {
	w.mu.Lock()
	defer w.mu.Unlock()

	p, ok := w.players[playerID]
	if !ok || p.dead {
		return BuffEvent{}, false
	}

	var duration float64
	switch buffID {
	case BuffBattleShout:
		duration = BattleShoutDuration
	case BuffIronSkin:
		duration = IronSkinDuration
	case BuffHunterMark:
		duration = HunterMarkDuration
		if markTarget <= 0 {
			return BuffEvent{}, false
		}
	default:
		return BuffEvent{}, false
	}

	for i := range p.buffs {
		if p.buffs[i].id == buffID {
			p.buffs[i].remain = duration
			p.buffs[i].duration = duration
			p.buffs[i].markTarget = markTarget
			return BuffEvent{
				PlayerID: playerID, BuffID: buffID,
				Remaining: duration, Duration: duration, MarkTargetID: markTarget,
			}, true
		}
	}

	p.buffs = append(p.buffs, playerBuff{
		id: buffID, remain: duration, duration: duration, markTarget: markTarget,
	})
	return BuffEvent{
		PlayerID: playerID, BuffID: buffID,
		Remaining: duration, Duration: duration, MarkTargetID: markTarget,
	}, true
}

func (w *World) tickPlayerBuffs(dt float64) []BuffEvent {
	w.mu.Lock()
	defer w.mu.Unlock()

	var expired []BuffEvent
	for _, p := range w.players {
		if len(p.buffs) == 0 {
			continue
		}
		kept := p.buffs[:0]
		for _, b := range p.buffs {
			b.remain -= dt
			if b.remain > 0 {
				kept = append(kept, b)
				continue
			}
			expired = append(expired, BuffEvent{
				PlayerID: p.id, BuffID: b.id, Remaining: 0, Duration: 0, MarkTargetID: b.markTarget,
			})
		}
		p.buffs = kept
	}
	return expired
}

// BuffsForSave returns paused buff snapshots (remaining time at logout).
func (w *World) BuffsForSave(playerID int64) []SavedBuff {
	w.mu.RLock()
	defer w.mu.RUnlock()
	p, ok := w.players[playerID]
	if !ok || len(p.buffs) == 0 {
		return nil
	}
	out := make([]SavedBuff, 0, len(p.buffs))
	for _, b := range p.buffs {
		if b.remain <= 0 {
			continue
		}
		out = append(out, SavedBuff{
			ID: b.id, Remain: b.remain, Duration: b.duration, MarkTargetID: b.markTarget,
		})
	}
	return out
}

// RestorePlayerBuffs re-applies paused buffs after login. Timers resume from Remain.
func (w *World) RestorePlayerBuffs(playerID int64, saved []SavedBuff) []BuffEvent {
	w.mu.Lock()
	defer w.mu.Unlock()

	p, ok := w.players[playerID]
	if !ok || p.dead || len(saved) == 0 {
		return nil
	}

	events := make([]BuffEvent, 0, len(saved))
	for _, s := range saved {
		if s.Remain <= 0 || s.ID == "" {
			continue
		}
		switch s.ID {
		case BuffBattleShout, BuffIronSkin:
		case BuffHunterMark:
			if s.MarkTargetID <= 0 {
				continue
			}
		default:
			continue
		}
		dur := s.Duration
		if dur < s.Remain {
			dur = s.Remain
		}
		replaced := false
		for i := range p.buffs {
			if p.buffs[i].id != s.ID {
				continue
			}
			p.buffs[i].remain = s.Remain
			p.buffs[i].duration = dur
			p.buffs[i].markTarget = s.MarkTargetID
			replaced = true
			break
		}
		if !replaced {
			p.buffs = append(p.buffs, playerBuff{
				id: s.ID, remain: s.Remain, duration: dur, markTarget: s.MarkTargetID,
			})
		}
		events = append(events, BuffEvent{
			PlayerID: playerID, BuffID: s.ID,
			Remaining: s.Remain, Duration: dur, MarkTargetID: s.MarkTargetID,
		})
	}
	return events
}

func (w *World) DamageDealtMultiplier(attackerID int64) float64 {
	w.mu.RLock()
	defer w.mu.RUnlock()
	p, ok := w.players[attackerID]
	if !ok {
		return 1
	}
	for _, b := range p.buffs {
		if b.id == BuffBattleShout {
			return 1 + BattleShoutDamageBonus
		}
	}
	return 1
}

func (w *World) DamageTakenMultiplier(targetID int64) float64 {
	w.mu.RLock()
	defer w.mu.RUnlock()
	p, ok := w.players[targetID]
	if !ok {
		return 1
	}
	for _, b := range p.buffs {
		if b.id == BuffIronSkin {
			return 1 - IronSkinDamageReduce
		}
	}
	return 1
}

// NearestEnemyInCone finds the closest living enemy within range in a facing cone.
func (w *World) NearestEnemyInCone(selfID int64, dirX, dirY, rangePx, minDot float64) int64 {
	w.mu.RLock()
	defer w.mu.RUnlock()
	self, ok := w.players[selfID]
	if !ok {
		return 0
	}
	var bestID int64
	bestDist := rangePx * rangePx
	for id, other := range w.players {
		if id == selfID || other.dead {
			continue
		}
		dx := other.x - self.x
		dy := other.y - self.y
		distSq := dx*dx + dy*dy
		if distSq > bestDist || distSq < 1 {
			continue
		}
		l := math.Hypot(dx, dy)
		if l < 0.001 {
			continue
		}
		dot := (dx/l)*dirX + (dy/l)*dirY
		if dot < minDot {
			continue
		}
		bestDist = distSq
		bestID = id
	}
	return bestID
}
