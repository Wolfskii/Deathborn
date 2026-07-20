package game

import (
	"math"
	"math/rand"
)

const (
	mobIDStart         = int64(-2000)
	mobHitPadding      = 1.12
	playerCombatRadius = 12.0
	mobMeleeSwingTime  = 0.45
	mobMeleeImpactTime = 0.24 // damage on the forward swing frame
	mobMeleeCooldown   = 2.0
)

type mob struct {
	id          int64
	defID       string
	name        string
	category    NpcCategory
	disposition NpcDisposition
	spriteID    string
	x, y        float64
	spawnX      float64
	spawnY      float64
	hp          float64
	hpMax       float64
	speed       float64
	radius      float64
	wander      bool
	leash       float64
	aggro       bool
	aggroRange  float64
	meleeDamage int
	meleeReach  float64 // scaled px from mob center to player center at sword contact
	hitHalfW    float64
	hitHalfH    float64
	hitCenterY  float64
	dirX        float64
	dirY        float64
	wanderTimer float64
	targetID    int64
	attackT     float64
	action      string
	actionT     float64
	pendingMelee bool
	meleeImpactT float64
}

type mobManager struct {
	nextID int64
	mobs   map[int64]*mob
}

func newMobManager() *mobManager {
	return &mobManager{
		nextID: mobIDStart,
		mobs:   make(map[int64]*mob),
	}
}

type mobSpawnPoint struct {
	defID string
	tx    int
	ty    int
}

func (w *World) mobSpawnPoints() []mobSpawnPoint {
	return []mobSpawnPoint{
		{defID: "forest_skeleton", tx: 130, ty: 210},
		{defID: "forest_skeleton", tx: 165, ty: 175},
		// Farm RPG slimes — color/size rolled at spawn
		{defID: "forest_slime", tx: 95, ty: 195},
		{defID: "forest_slime", tx: 200, ty: 230},
		{defID: "forest_slime", tx: 118, ty: 240},
		{defID: "forest_slime", tx: 155, ty: 195},
		{defID: "forest_slime", tx: 210, ty: 190},
		{defID: "forest_slime", tx: 88, ty: 220},
		{defID: "forest_slime", tx: 175, ty: 255},
		{defID: "forest_slime", tx: 140, ty: 165},
		{defID: "forest_orc", tx: 145, ty: 250},
		{defID: "forest_orc", tx: 220, ty: 160},
		{defID: "wild_bat", tx: 110, ty: 165},
		{defID: "wild_bat", tx: 185, ty: 205},
		{defID: "town_guard", tx: 178, ty: 118},
		{defID: "town_guard", tx: 192, ty: 122},
		{defID: "town_priest", tx: 185, ty: 108},
		{defID: "town_wizard", tx: 172, ty: 112},
	}
}

func (w *World) ensureMobsInitialized() {
	if w.mobMgr == nil {
		w.mobMgr = newMobManager()
	}
	if len(w.mobMgr.mobs) > 0 {
		return
	}
	w.spawnInitialMobsLocked()
}

func (w *World) spawnInitialMobsLocked() {
	ts := 16.0
	if w.terrain != nil {
		ts = w.terrain.TileSize
	}
	for _, sp := range w.mobSpawnPoints() {
		def, ok := lookupNpcDef(sp.defID)
		if !ok {
			continue
		}
		x := (float64(sp.tx) + 0.5) * ts
		y := (float64(sp.ty) + 0.5) * ts
		if w.terrain != nil && !w.terrain.CanWalk(x, y, 0) {
			continue
		}
		if def.category.blocksTowns() {
			if w.zones != nil && w.zones.InMonsterExclusion(x, y) {
				continue
			}
			if w.housing != nil && w.housing.Contains(x, y) {
				continue
			}
		} else if w.zones != nil && !w.zones.InSafeArea(x, y) {
			// Friendly town NPCs only spawn inside safe towns.
			continue
		}
		w.spawnMobLocked(def, x, y)
	}
}

func (w *World) spawnMobLocked(def npcDef, x, y float64) {
	id := w.mobMgr.nextID
	w.mobMgr.nextID--
	scale := w.worldScale()

	spriteID := def.spriteID
	name := def.name
	hpMax := def.hpMax
	radius := def.radius
	hitHalfW := def.hitHalfW
	hitHalfH := def.hitHalfH
	hitCenterY := def.hitCenterY
	meleeDamage := def.meleeDamage

	if def.id == "forest_slime" {
		variant := rollFarmRpgSlimeVariant()
		spriteID = variant.spriteID
		name = variant.name
		hpMax = def.hpMax * variant.hpMul
		radius = def.radius * variant.radiusMul
		hitHalfW = def.hitHalfW * variant.hitMul
		hitHalfH = def.hitHalfH * variant.hitMul
		hitCenterY = def.hitCenterY * variant.hitMul
		meleeDamage = int(math.Max(1, math.Round(float64(def.meleeDamage)*variant.damageMul)))
	}

	m := &mob{
		id: id, defID: def.id, name: name,
		category: def.category, disposition: def.disposition, spriteID: spriteID,
		x: x, y: y, spawnX: x, spawnY: y,
		hp: hpMax, hpMax: hpMax,
		speed: def.speed * scale, radius: radius * scale,
		wander: def.wander, leash: def.leash * scale,
		aggro: def.aggro, aggroRange: def.aggroRange * scale,
		meleeDamage: meleeDamage,
		meleeReach:  def.meleeReach * scale,
		hitHalfW:    hitHalfW * scale * mobHitPadding,
		hitHalfH:    hitHalfH * scale * mobHitPadding,
		hitCenterY:  hitCenterY * scale,
		dirY: 1,
	}
	w.mobMgr.mobs[id] = m
}

var farmRpgSlimeColors = []string{"blue", "black", "golden", "green", "pink", "purple"}
var farmRpgSlimeSizes = []string{"small", "normal", "big"}

type farmRpgSlimeVariant struct {
	spriteID  string
	name      string
	hpMul     float64
	radiusMul float64
	hitMul    float64
	damageMul float64
}

func rollFarmRpgSlimeVariant() farmRpgSlimeVariant {
	color := farmRpgSlimeColors[rand.Intn(len(farmRpgSlimeColors))]
	size := farmRpgSlimeSizes[rand.Intn(len(farmRpgSlimeSizes))]
	v := farmRpgSlimeVariant{
		spriteID: "slime_" + color + "_" + size,
		name:     farmRpgSlimeColorTitle(color) + " Slime",
	}
	switch size {
	case "small":
		v.hpMul, v.radiusMul, v.hitMul, v.damageMul = 0.7, 0.75, 0.75, 0.75
		v.name = "Small " + v.name
	case "big":
		v.hpMul, v.radiusMul, v.hitMul, v.damageMul = 1.55, 1.35, 1.35, 1.4
		v.name = "Big " + v.name
	default:
		v.hpMul, v.radiusMul, v.hitMul, v.damageMul = 1, 1, 1, 1
	}
	return v
}

func farmRpgSlimeColorTitle(color string) string {
	switch color {
	case "blue":
		return "Blue"
	case "black":
		return "Black"
	case "golden":
		return "Golden"
	case "green":
		return "Green"
	case "pink":
		return "Pink"
	case "purple":
		return "Purple"
	default:
		return "Slime"
	}
}

func (w *World) TickMobs(dt float64) []BossEvent {
	w.mu.Lock()
	defer w.mu.Unlock()
	w.ensureMobsInitialized()
	var out []BossEvent
	for _, m := range w.mobMgr.mobs {
		out = append(out, w.tickMobLocked(m, dt)...)
	}
	return out
}

func (w *World) tickMobLocked(m *mob, dt float64) []BossEvent {
	if m.actionT > 0 {
		m.actionT -= dt
		if m.actionT <= 0 {
			m.action = ""
		}
	}
	if m.attackT > 0 {
		m.attackT -= dt
	}

	if m.aggro && m.speed > 0 && m.meleeDamage > 0 {
		if events := w.tickMobCombatLocked(m, dt); len(events) > 0 || m.targetID != 0 {
			return events
		}
	}

	if !m.wander || m.speed <= 0 {
		return nil
	}

	m.wanderTimer -= dt
	if m.wanderTimer <= 0 {
		m.wanderTimer = 2.5 + rand.Float64()*3.5
		angle := rand.Float64() * math.Pi * 2
		m.dirX = math.Cos(angle)
		m.dirY = math.Sin(angle)
	}

	if math.Hypot(m.x-m.spawnX, m.y-m.spawnY) > m.leash {
		dx := m.spawnX - m.x
		dy := m.spawnY - m.y
		dist := math.Hypot(dx, dy)
		if dist > 0.01 {
			m.dirX = dx / dist
			m.dirY = dy / dist
		}
	}

	dx := m.dirX * m.speed * dt
	dy := m.dirY * m.speed * dt
	nx := m.x + dx
	ny := m.y + dy

	if m.category.blocksTowns() {
		if w.zones != nil && w.zones.InMonsterExclusion(nx, ny) {
			nx, ny = w.zones.PushOutOfMonsterExclusion(nx, ny)
			m.wanderTimer = 0
		}
		if w.housing != nil && w.housing.Contains(nx, ny) {
			return nil
		}
	}

	if w.terrain != nil {
		if !w.terrain.CanWalk(nx, ny, m.radius) {
			m.wanderTimer = 0
			return nil
		}
		nx, ny = w.terrain.ResolveMove(m.x, m.y, nx-m.x, ny-m.y)
		if !w.terrain.CanWalk(nx, ny, m.radius) {
			m.wanderTimer = 0
			return nil
		}
	}

	if m.category.blocksTowns() && w.zones != nil && w.zones.InMonsterExclusion(nx, ny) {
		return nil
	}

	m.x, m.y = nx, ny
	return nil
}

func (w *World) tickMobCombatLocked(m *mob, dt float64) []BossEvent {
	target, dist := w.resolveMobTargetLocked(m)
	if target == nil {
		m.pendingMelee = false
		return nil
	}

	scale := w.worldScale()
	if dist > m.leash {
		m.pendingMelee = false
		w.moveMobTowardLocked(m, m.spawnX, m.spawnY, dt*0.85)
		if math.Hypot(m.x-m.spawnX, m.y-m.spawnY) < 12*scale {
			m.targetID = 0
		}
		return nil
	}

	if m.pendingMelee {
		m.meleeImpactT -= dt
		if m.meleeImpactT <= 0 {
			m.pendingMelee = false
			if w.mobMeleeContact(m, target) {
				if ev := w.mobMeleeHitLocked(m, target); ev != nil {
					return []BossEvent{*ev}
				}
			}
		}
		return nil
	}

	if m.actionT > 0 {
		return nil
	}

	bodyDist := distPointToAabb(target.x, target.y, m.x, m.y+m.hitCenterY, m.hitHalfW, m.hitHalfH)
	contact := w.mobMeleeContactDist(m)
	swingStart := contact + 4*scale
	if bodyDist <= swingStart {
		if m.attackT <= 0 {
			dx := target.x - m.x
			dy := target.y - m.y
			if d := math.Hypot(dx, dy); d > 0.01 {
				m.dirX = dx / d
				m.dirY = dy / d
			}
			m.action = "melee"
			m.actionT = mobMeleeSwingTime
			m.attackT = mobMeleeCooldown
			m.pendingMelee = true
			m.meleeImpactT = mobMeleeImpactTime
		}
		return nil
	}

	w.moveMobTowardLocked(m, target.x, target.y, dt)
	return nil
}

func (w *World) mobMeleeContactDist(m *mob) float64 {
	return m.meleeReach + playerCombatRadius*w.worldScale()
}

func (w *World) mobMeleeContact(m *mob, target *player) bool {
	return distPointToAabb(target.x, target.y, m.x, m.y+m.hitCenterY, m.hitHalfW, m.hitHalfH) <= w.mobMeleeContactDist(m)
}

func (w *World) resolveMobTargetLocked(m *mob) (*player, float64) {
	const loseAggroMul = 1.35

	if m.targetID != 0 {
		if p, ok := w.players[m.targetID]; ok && !p.dead && !w.playerInSafeHavenLocked(p) {
			dist := math.Hypot(p.x-m.x, p.y-m.y)
			if dist > m.aggroRange*loseAggroMul {
				m.targetID = 0
				m.pendingMelee = false
			} else {
				return p, dist
			}
		} else {
			m.targetID = 0
			m.pendingMelee = false
		}
	}

	target, dist := w.nearestPlayerLocked(m.x, m.y, m.aggroRange)
	if target != nil {
		m.targetID = target.id
	}
	return target, dist
}

func (w *World) moveMobTowardLocked(m *mob, tx, ty float64, dt float64) {
	dx := tx - m.x
	dy := ty - m.y
	dist := math.Hypot(dx, dy)
	if dist < 1 {
		return
	}
	dx /= dist
	dy /= dist
	m.dirX, m.dirY = dx, dy
	step := m.speed * dt
	if step > dist {
		step = dist
	}
	nx := m.x + dx*step
	ny := m.y + dy*step
	if m.category.blocksTowns() {
		if w.zones != nil && w.zones.InMonsterExclusion(nx, ny) {
			nx, ny = w.zones.PushOutOfMonsterExclusion(nx, ny)
		}
		if w.housing != nil && w.housing.Contains(nx, ny) {
			return
		}
	}
	if w.terrain != nil {
		if !w.terrain.CanWalk(nx, ny, m.radius) {
			return
		}
		nx, ny = w.terrain.ResolveMove(m.x, m.y, nx-m.x, ny-m.y)
		if !w.terrain.CanWalk(nx, ny, m.radius) {
			return
		}
	} else {
		m.x, m.y = nx, ny
		return
	}
	if m.category.blocksTowns() && w.zones != nil && w.zones.InMonsterExclusion(nx, ny) {
		return
	}
	m.x, m.y = nx, ny
}

func (w *World) mobMeleeHitLocked(m *mob, target *player) *BossEvent {
	if w.playerInSafeHavenLocked(target) || m.meleeDamage <= 0 {
		return nil
	}
	hp, hpMax, justDied, ok := w.applyPlayerDamageLocked(target.id, m.meleeDamage)
	if !ok {
		return nil
	}
	return &BossEvent{
		Type: "player_hit", NpcID: m.id, PlayerID: target.id, Damage: m.meleeDamage,
		Name: "mob_melee",
		Hp:   hp, HpMax: hpMax, JustDied: justDied,
	}
}

func (w *World) appendMobSnapshotsLocked(out []NpcState) []NpcState {
	if w.mobMgr == nil || len(w.mobMgr.mobs) == 0 {
		return out
	}
	for _, m := range w.mobMgr.mobs {
		out = append(out, NpcState{
			ID: m.id, DefID: m.defID, Name: m.name,
			Category: string(m.category), Disposition: string(m.disposition),
			SpriteID: m.spriteID,
			X:        m.x, Y: m.y, Hp: m.hp, HpMax: m.hpMax,
			IsBoss: false, Action: m.action, DirX: m.dirX, DirY: m.dirY,
		})
	}
	return out
}

func (w *World) findMobLocked(npcID int64) (*mob, bool) {
	if w.mobMgr == nil {
		return nil, false
	}
	m, ok := w.mobMgr.mobs[npcID]
	return m, ok
}

func (w *World) validateMobHitLocked(attackerID, npcID int64, ability string) bool {
	maxR := MaxHitRange(ability)
	if maxR <= 0 {
		return false
	}
	a, okA := w.players[attackerID]
	m, okM := w.findMobLocked(npcID)
	if !okA || !okM {
		return false
	}
	if m.disposition == NpcFriendly {
		return false
	}
	cx := m.x
	cy := m.y + m.hitCenterY
	if m.hitHalfW <= 0 || m.hitHalfH <= 0 {
		dx := a.x - cx
		dy := a.y - cy
		return dx*dx+dy*dy <= (maxR+m.radius)*(maxR+m.radius)
	}
	return distSqPointToAabb(a.x, a.y, cx, cy, m.hitHalfW, m.hitHalfH) <= maxR*maxR
}

func distPointToAabb(px, py, cx, cy, halfW, halfH float64) float64 {
	dx := math.Abs(px-cx) - halfW
	if dx < 0 {
		dx = 0
	}
	dy := math.Abs(py-cy) - halfH
	if dy < 0 {
		dy = 0
	}
	return math.Hypot(dx, dy)
}

func distSqPointToAabb(px, py, cx, cy, halfW, halfH float64) float64 {
	dx := math.Abs(px-cx) - halfW
	if dx < 0 {
		dx = 0
	}
	dy := math.Abs(py-cy) - halfH
	if dy < 0 {
		dy = 0
	}
	return dx*dx + dy*dy
}

func (w *World) applyMobDamageLocked(npcID int64, damage int) (hp, hpMax float64, justDied bool, ok bool) {
	if damage <= 0 {
		return 0, 0, false, false
	}
	m, ok := w.findMobLocked(npcID)
	if !ok || m.disposition == NpcFriendly {
		return 0, 0, false, false
	}
	m.hp -= float64(damage)
	hp, hpMax = m.hp, m.hpMax
	if m.hp <= 0 {
		m.hp = 0
		hp = 0
		justDied = true
		delete(w.mobMgr.mobs, npcID)
	}
	return hp, hpMax, justDied, true
}

func (w *World) mobPositionLocked(npcID int64) (x, y float64, ok bool) {
	m, ok := w.findMobLocked(npcID)
	if !ok {
		return 0, 0, false
	}
	return m.x, m.y, true
}
