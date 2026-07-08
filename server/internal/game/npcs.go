package game

import (
	"math"
	"math/rand"
)

const mobIDStart = int64(-2000)

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
	dirX        float64
	dirY        float64
	wanderTimer float64
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
		{defID: "forest_slime", tx: 95, ty: 195},
		{defID: "forest_slime", tx: 200, ty: 230},
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
	m := &mob{
		id: id, defID: def.id, name: def.name,
		category: def.category, disposition: def.disposition, spriteID: def.spriteID,
		x: x, y: y, spawnX: x, spawnY: y,
		hp: def.hpMax, hpMax: def.hpMax,
		speed: def.speed * scale, radius: def.radius * scale,
		wander: def.wander, leash: def.leash * scale,
		dirY: 1,
	}
	w.mobMgr.mobs[id] = m
}

func (w *World) TickMobs(dt float64) {
	w.mu.Lock()
	defer w.mu.Unlock()
	w.ensureMobsInitialized()
	for _, m := range w.mobMgr.mobs {
		w.tickMobLocked(m, dt)
	}
}

func (w *World) tickMobLocked(m *mob, dt float64) {
	if !m.wander || m.speed <= 0 {
		return
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
			return
		}
	}

	if w.terrain != nil {
		if !w.terrain.CanWalk(nx, ny, m.radius) {
			m.wanderTimer = 0
			return
		}
		nx, ny = w.terrain.ResolveMove(m.x, m.y, nx-m.x, ny-m.y)
		if !w.terrain.CanWalk(nx, ny, m.radius) {
			m.wanderTimer = 0
			return
		}
	}

	if m.category.blocksTowns() && w.zones != nil && w.zones.InMonsterExclusion(nx, ny) {
		return
	}

	m.x, m.y = nx, ny
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
			X: m.x, Y: m.y, Hp: m.hp, HpMax: m.hpMax,
			IsBoss: false, DirX: m.dirX, DirY: m.dirY,
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
	dx := a.x - m.x
	dy := a.y - m.y
	return dx*dx+dy*dy <= (maxR+m.radius)*(maxR+m.radius)
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
