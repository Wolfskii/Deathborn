package game

import "math"

const (
	BuffBattleShout = "battle_shout"
	BuffIronSkin    = "iron_skin"
	BuffHunterMark  = "hunter_mark"

	BattleShoutDuration = 8.0
	IronSkinDuration    = 10.0
	HunterMarkDuration  = 900.0

	BattleShoutDamageBonus = 0.25
	IronSkinDamageReduce   = 0.30
)

type playerBuff struct {
	id         string
	remain     float64
	duration   float64
	markTarget int64
}

// BuffEvent is emitted when a buff is applied.
type BuffEvent struct {
	PlayerID     int64
	BuffID       string
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
				PlayerID: playerID, BuffID: buffID, Duration: duration, MarkTargetID: markTarget,
			}, true
		}
	}

	p.buffs = append(p.buffs, playerBuff{
		id: buffID, remain: duration, duration: duration, markTarget: markTarget,
	})
	return BuffEvent{
		PlayerID: playerID, BuffID: buffID, Duration: duration, MarkTargetID: markTarget,
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
				PlayerID: p.id, BuffID: b.id, Duration: 0, MarkTargetID: b.markTarget,
			})
		}
		p.buffs = kept
	}
	return expired
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
