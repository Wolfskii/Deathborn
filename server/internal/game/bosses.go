package game

import (
	"math"
	"math/rand"
)

const (
	maxWorldBosses    = 2
	bossSpawnInterval = 180.0 // seconds between spawn attempts
	bossLeashRadius    = 520.0
	bossAggroRadius    = 340.0
	bossMeleeReach     = 64.0
)

// NpcState is the wire view of an NPC/boss included in snapshots.
type NpcState struct {
	ID     int64   `json:"id"`
	DefID  string  `json:"defId"`
	Name   string  `json:"name"`
	X      float64 `json:"x"`
	Y      float64 `json:"y"`
	Hp     float64 `json:"hp"`
	HpMax  float64 `json:"hpMax"`
	IsBoss bool    `json:"isBoss"`
	Action string  `json:"action,omitempty"`
	DirX   float64 `json:"dirX,omitempty"`
	DirY   float64 `json:"dirY,omitempty"`
}

// WorldEventState describes an active world boss event.
type WorldEventState struct {
	Active  bool   `json:"active"`
	Name    string `json:"name,omitempty"`
	PvPOff  bool   `json:"pvpOff"`
	BossCnt int    `json:"bossCount"`
}

type bossDef struct {
	id       string
	name     string
	hpMax    float64
	speed    float64
	radius   float64
	aggro    float64
	leash    float64
	ability  string
	abilityCD float64
	abilityDmg int
	abilityRange float64
}

type boss struct {
	id       int64
	defID    string
	name     string
	x, y     float64
	spawnX   float64
	spawnY   float64
	hp       float64
	hpMax    float64
	speed    float64
	radius   float64
	aggro    float64
	leash    float64
	dirX     float64
	dirY     float64
	action   string
	actionT  float64
	ability  string
	abilityCD float64
	abilityDmg int
	abilityRange float64
	cdTimer  float64
	attackT  float64
	targetID int64
}

type bossManager struct {
	nextID       int64
	bosses       map[int64]*boss
	spawnTimer   float64
	eventActive  bool
	pendingEvents []BossEvent
}

type BossEvent struct {
	Type     string // spawn, death, world_event, player_hit, boss_action
	NpcID    int64
	DefID    string
	Name     string
	X, Y     float64
	PlayerID int64
	Damage   int
	Hp       float64
	HpMax    float64
	JustDied bool
	Active   bool
	BossCnt  int
}

func newBossManager() *bossManager {
	return &bossManager{
		nextID: -1000,
		bosses: make(map[int64]*boss),
	}
}

var bossDefs = []bossDef{
	{
		id: "iron_colossus", name: "Iron Colossus", hpMax: 1200, speed: 58,
		radius: 22, aggro: bossAggroRadius, leash: bossLeashRadius,
		ability: "ground_slam", abilityCD: 4.0, abilityDmg: 18, abilityRange: 120,
	},
	{
		id: "storm_wyrm", name: "Storm Wyrm", hpMax: 850, speed: 92,
		radius: 18, aggro: 380, leash: bossLeashRadius,
		ability: "lightning_bolt", abilityCD: 2.8, abilityDmg: 14, abilityRange: 300,
	},
	{
		id: "blight_herald", name: "Blight Herald", hpMax: 950, speed: 72,
		radius: 20, aggro: bossAggroRadius, leash: bossLeashRadius,
		ability: "poison_nova", abilityCD: 5.0, abilityDmg: 10, abilityRange: 100,
	},
}

func (w *World) bossSpawnPoints() [][2]float64 {
	ts := 16.0
	if w.terrain != nil {
		ts = w.terrain.TileSize
	}
	tile := func(tx, ty int) [2]float64 {
		return [2]float64{(float64(tx) + 0.5) * ts, (float64(ty) + 0.5) * ts}
	}
	return [][2]float64{
		tile(150, 160), tile(175, 95), tile(55, 130), tile(205, 215),
		tile(90, 240), tile(240, 170),
	}
}

func (w *World) TickBosses(dt float64) []BossEvent {
	if w.bossMgr == nil {
		w.bossMgr = newBossManager()
	}
	w.mu.Lock()
	defer w.mu.Unlock()

	var out []BossEvent
	w.bossMgr.spawnTimer += dt
	if w.bossMgr.spawnTimer >= bossSpawnInterval && len(w.bossMgr.bosses) < maxWorldBosses {
		w.bossMgr.spawnTimer = 0
		if w.trySpawnBossLocked() {
			out = append(out, w.bossMgr.pendingEvents...)
			w.bossMgr.pendingEvents = nil
		}
	}

	for id := range w.bossMgr.bosses {
		events := w.tickBossLocked(w.bossMgr.bosses[id], dt)
		out = append(out, events...)
	}

	w.bossMgr.eventActive = len(w.bossMgr.bosses) > 0
	return out
}

func (w *World) trySpawnBossLocked() bool {
	points := w.bossSpawnPoints()
	rand.Shuffle(len(points), func(i, j int) { points[i], points[j] = points[j], points[i] })
	for _, p := range points {
		if w.zones != nil && w.zones.InMonsterExclusion(p[0], p[1]) {
			continue
		}
		if !w.CanWalk(p[0], p[1]) {
			continue
		}
		def := bossDefs[rand.Intn(len(bossDefs))]
		w.spawnBossLocked(def, p[0], p[1])
		return true
	}
	return false
}

func (w *World) spawnBossLocked(def bossDef, x, y float64) {
	id := w.bossMgr.nextID
	w.bossMgr.nextID--
	b := &boss{
		id: id, defID: def.id, name: def.name,
		x: x, y: y, spawnX: x, spawnY: y,
		hp: def.hpMax, hpMax: def.hpMax,
		speed: def.speed, radius: def.radius,
		aggro: def.aggro, leash: def.leash,
		ability: def.ability, abilityCD: def.abilityCD,
		abilityDmg: def.abilityDmg, abilityRange: def.abilityRange,
		dirY: 1,
	}
	w.bossMgr.bosses[id] = b
	w.bossMgr.pendingEvents = append(w.bossMgr.pendingEvents, BossEvent{
		Type: "spawn", NpcID: id, DefID: def.id, Name: def.name, X: x, Y: y,
	})
	w.queueWorldEventLocked()
}

func (w *World) queueWorldEventLocked() {
	active := len(w.bossMgr.bosses) > 0
	w.bossMgr.pendingEvents = append(w.bossMgr.pendingEvents, BossEvent{
		Type: "world_event", Active: active, BossCnt: len(w.bossMgr.bosses),
		Name: "World Boss Awakens",
	})
}

func (w *World) tickBossLocked(b *boss, dt float64) []BossEvent {
	var events []BossEvent
	if b.actionT > 0 {
		b.actionT -= dt
		if b.actionT <= 0 {
			b.action = ""
		}
	}
	if b.attackT > 0 {
		b.attackT -= dt
	}
	b.cdTimer -= dt

	target, dist := w.nearestPlayerLocked(b.x, b.y, b.aggro)
	if target == nil {
		b.targetID = 0
		// Leash home if too far from spawn
		if math.Hypot(b.x-b.spawnX, b.y-b.spawnY) > 40 {
			w.moveBossTowardLocked(b, b.spawnX, b.spawnY, dt*0.6)
		}
		return events
	}

	b.targetID = target.id
	if dist > b.leash {
		w.moveBossTowardLocked(b, b.spawnX, b.spawnY, dt)
		return events
	}

	if dist <= bossMeleeReach+b.radius+12 {
		if b.attackT <= 0 {
			b.action = "melee"
			b.actionT = 0.45
			b.attackT = 1.2
			if dmgEv := w.bossMeleeHitLocked(b, target); dmgEv != nil {
				events = append(events, *dmgEv)
			}
		}
	} else {
		w.moveBossTowardLocked(b, target.x, target.y, dt)
	}

	if b.cdTimer <= 0 && dist <= b.abilityRange {
		b.cdTimer = b.abilityCD
		b.action = b.ability
		b.actionT = 0.6
		abilityEvents := w.bossAbilityLocked(b)
		events = append(events, abilityEvents...)
	}

	return events
}

func (w *World) nearestPlayerLocked(x, y, radius float64) (*player, float64) {
	var best *player
	bestDist := radius * radius
	for _, p := range w.players {
		if p.dead {
			continue
		}
		if w.zones != nil && w.zones.InSafeArea(p.x, p.y) {
			continue
		}
		dx := p.x - x
		dy := p.y - y
		d2 := dx*dx + dy*dy
		if d2 < bestDist {
			bestDist = d2
			best = p
		}
	}
	if best == nil {
		return nil, 0
	}
	return best, math.Sqrt(bestDist)
}

func (w *World) moveBossTowardLocked(b *boss, tx, ty float64, dt float64) {
	dx := tx - b.x
	dy := ty - b.y
	dist := math.Hypot(dx, dy)
	if dist < 1 {
		return
	}
	dx /= dist
	dy /= dist
	b.dirX, b.dirY = dx, dy
	step := b.speed * dt
	if step > dist {
		step = dist
	}
	nx := b.x + dx*step
	ny := b.y + dy*step
	if w.zones != nil && w.zones.InMonsterExclusion(nx, ny) {
		nx, ny = w.zones.PushOutOfMonsterExclusion(nx, ny)
	}
	if w.terrain != nil {
		nx, ny = w.terrain.ResolveMove(b.x, b.y, nx-b.x, ny-b.y)
	} else {
		b.x, b.y = nx, ny
		return
	}
	if w.zones != nil && w.zones.InMonsterExclusion(nx, ny) {
		return
	}
	b.x, b.y = nx, ny
}

func (w *World) bossMeleeHitLocked(b *boss, target *player) *BossEvent {
	if w.zones != nil && w.zones.InSafeArea(target.x, target.y) {
		return nil
	}
	damage := 8
	hp, hpMax, justDied, ok := w.applyPlayerDamageLocked(target.id, damage)
	if !ok {
		return nil
	}
	ev := &BossEvent{
		Type: "player_hit", NpcID: b.id, PlayerID: target.id, Damage: damage,
		Name: "boss_melee",
		Hp: hp, HpMax: hpMax, JustDied: justDied,
	}
	return ev
}

func (w *World) bossAbilityLocked(b *boss) []BossEvent {
	var events []BossEvent
	switch b.ability {
	case "ground_slam":
		for _, p := range w.players {
			if p.dead || (w.zones != nil && w.zones.InSafeArea(p.x, p.y)) {
				continue
			}
			if math.Hypot(p.x-b.x, p.y-b.y) <= b.abilityRange {
				if hp, hpMax, justDied, ok := w.applyPlayerDamageLocked(p.id, b.abilityDmg); ok {
					events = append(events, BossEvent{
						Type: "player_hit", NpcID: b.id, PlayerID: p.id, Damage: b.abilityDmg,
						Name: b.ability,
						Hp: hp, HpMax: hpMax, JustDied: justDied,
					})
				}
			}
		}
	case "lightning_bolt":
		target, _ := w.nearestPlayerLocked(b.x, b.y, b.abilityRange)
		if target != nil && (w.zones == nil || !w.zones.InSafeArea(target.x, target.y)) {
			if hp, hpMax, justDied, ok := w.applyPlayerDamageLocked(target.id, b.abilityDmg); ok {
				events = append(events, BossEvent{
					Type: "player_hit", NpcID: b.id, PlayerID: target.id, Damage: b.abilityDmg,
					Name: b.ability,
					Hp: hp, HpMax: hpMax, JustDied: justDied,
				})
			}
			events = append(events, BossEvent{Type: "boss_action", NpcID: b.id, Name: "lightning_bolt"})
		}
	case "poison_nova":
		for _, p := range w.players {
			if p.dead || (w.zones != nil && w.zones.InSafeArea(p.x, p.y)) {
				continue
			}
			if math.Hypot(p.x-b.x, p.y-b.y) <= b.abilityRange {
				if hp, hpMax, justDied, ok := w.applyPlayerDamageLocked(p.id, b.abilityDmg); ok {
					events = append(events, BossEvent{
						Type: "player_hit", NpcID: b.id, PlayerID: p.id, Damage: b.abilityDmg,
						Name: b.ability,
						Hp: hp, HpMax: hpMax, JustDied: justDied,
					})
				}
			}
		}
		events = append(events, BossEvent{Type: "boss_action", NpcID: b.id, Name: "poison_nova"})
	}
	return events
}

func (w *World) applyPlayerDamageLocked(targetID int64, damage int) (hp, hpMax float64, justDied, ok bool) {
	if damage <= 0 {
		return 0, 0, false, false
	}
	p, ok := w.players[targetID]
	if !ok || p.dead {
		return 0, 0, false, false
	}
	p.hp -= float64(damage)
	if p.hp <= 0 {
		p.hp = 0
		if !p.dead {
			p.dead = true
			justDied = true
		}
	}
	return p.hp, p.hpMax, justDied, true
}

func (w *World) NpcSnapshot() []NpcState {
	w.mu.RLock()
	defer w.mu.RUnlock()
	if w.bossMgr == nil || len(w.bossMgr.bosses) == 0 {
		return nil
	}
	out := make([]NpcState, 0, len(w.bossMgr.bosses))
	for _, b := range w.bossMgr.bosses {
		out = append(out, NpcState{
			ID: b.id, DefID: b.defID, Name: b.name,
			X: b.x, Y: b.y, Hp: b.hp, HpMax: b.hpMax,
			IsBoss: true, Action: b.action,
			DirX: b.dirX, DirY: b.dirY,
		})
	}
	return out
}

func (w *World) WorldEventSnapshot() *WorldEventState {
	w.mu.RLock()
	defer w.mu.RUnlock()
	if w.bossMgr == nil || !w.bossMgr.eventActive {
		return &WorldEventState{Active: false, PvPOff: false}
	}
	return &WorldEventState{
		Active: true, Name: "World Boss Event",
		PvPOff: true, BossCnt: len(w.bossMgr.bosses),
	}
}

func (w *World) WorldBossEventActive() bool {
	w.mu.RLock()
	defer w.mu.RUnlock()
	return w.bossMgr != nil && w.bossMgr.eventActive
}

func (w *World) ValidateNpcHit(attackerID, npcID int64, ability string) bool {
	maxR := MaxHitRange(ability)
	if maxR <= 0 {
		return false
	}
	w.mu.RLock()
	defer w.mu.RUnlock()
	if w.bossMgr == nil {
		return false
	}
	a, okA := w.players[attackerID]
	b, okB := w.bossMgr.bosses[npcID]
	if !okA || !okB {
		return false
	}
	dx := a.x - b.x
	dy := a.y - b.y
	return dx*dx+dy*dy <= (maxR+b.radius)*(maxR+b.radius)
}

func (w *World) ApplyDamageToNpc(npcID int64, damage int) (hp, hpMax float64, justDied bool, ok bool) {
	if damage <= 0 {
		return 0, 0, false, false
	}
	w.mu.Lock()
	defer w.mu.Unlock()
	if w.bossMgr == nil {
		return 0, 0, false, false
	}
	b, ok := w.bossMgr.bosses[npcID]
	if !ok {
		return 0, 0, false, false
	}
	b.hp -= float64(damage)
	hp, hpMax = b.hp, b.hpMax
	if b.hp <= 0 {
		b.hp = 0
		hp = 0
		justDied = true
		x, y, defID, name := b.x, b.y, b.defID, b.name
		delete(w.bossMgr.bosses, npcID)
		w.bossMgr.pendingEvents = append(w.bossMgr.pendingEvents, BossEvent{
			Type: "death", NpcID: npcID, DefID: defID, Name: name, X: x, Y: y,
		})
		if len(w.bossMgr.bosses) == 0 {
			w.bossMgr.eventActive = false
			w.bossMgr.pendingEvents = append(w.bossMgr.pendingEvents, BossEvent{
				Type: "world_event", Active: false, BossCnt: 0, Name: "World Boss Event Ended",
			})
		}
	}
	return hp, hpMax, justDied, true
}

func (w *World) NpcPosition(npcID int64) (x, y float64, ok bool) {
	w.mu.RLock()
	defer w.mu.RUnlock()
	if w.bossMgr == nil {
		return 0, 0, false
	}
	b, ok := w.bossMgr.bosses[npcID]
	if !ok {
		return 0, 0, false
	}
	return b.x, b.y, true
}

// DrainPendingBossEvents returns queued spawn/death/world_event messages.
func (w *World) DrainPendingBossEvents() []BossEvent {
	w.mu.Lock()
	defer w.mu.Unlock()
	if w.bossMgr == nil || len(w.bossMgr.pendingEvents) == 0 {
		return nil
	}
	out := w.bossMgr.pendingEvents
	w.bossMgr.pendingEvents = nil
	return out
}

// PlayerDamageResult returns HP after boss damage and whether the player died.
func (w *World) ApplyBossDamageToPlayer(playerID int64, damage int) (hp, hpMax float64, justDied bool, ok bool) {
	w.mu.Lock()
	defer w.mu.Unlock()
	return w.applyPlayerDamageLocked(playerID, damage)
}
