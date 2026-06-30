package net

import (
	"context"
	"encoding/json"
	"errors"
	"log"
	"math"
	"net/http"
	"strconv"
	"strings"
	"sync"
	"time"

	"github.com/deathborn/server/internal/auth"
	"github.com/deathborn/server/internal/db"
	"github.com/deathborn/server/internal/game"
	"github.com/deathborn/server/internal/skills"
	"github.com/gorilla/websocket"
)

const (
	writeTimeout = 10 * time.Second
	sendBuffer   = 64
)

var upgrader = websocket.Upgrader{
	// The Godot client is not a browser; allow any origin. Tighten this if the
	// client is ever exported to the web.
	CheckOrigin: func(r *http.Request) bool { return true },
}

// Client is one connected player's socket plus session state.
type Client struct {
	hub  *Hub
	conn *websocket.Conn
	send chan []byte

	closed    chan struct{}
	closeOnce sync.Once

	accountID   int64
	characterID int64
	spawned     bool
}

func (c *Client) markUnspawned() {
	c.spawned = false
}

// trySend queues a message without blocking. Returns false only if the buffer
// is full (a slow client), in which case the hub drops the connection.
func (c *Client) trySend(msg []byte) bool {
	select {
	case c.send <- msg:
		return true
	case <-c.closed:
		return true
	default:
		return false
	}
}

// safeSend queues a message, blocking until buffered or the client closes.
func (c *Client) safeSend(msg []byte) {
	select {
	case c.send <- msg:
	case <-c.closed:
	}
}

// close terminates the connection exactly once. The send channel is never
// closed, so concurrent senders can never panic.
func (c *Client) close() {
	c.closeOnce.Do(func() {
		close(c.closed)
		_ = c.conn.Close()
	})
}

// ServeWS authenticates the JWT from the query string, upgrades the connection,
// and either spawns the account's living character or asks the client to create
// one.
func ServeWS(hub *Hub, database *db.DB, secret string) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		accountID, err := auth.ParseToken(secret, r.URL.Query().Get("token"))
		if err != nil {
			http.Error(w, "unauthorized", http.StatusUnauthorized)
			return
		}

		conn, err := upgrader.Upgrade(w, r, nil)
		if err != nil {
			log.Printf("ws upgrade failed account_id=%d: %v", accountID, err)
			return // upgrader already wrote the response
		}

		c := &Client{
			hub:       hub,
			conn:      conn,
			send:      make(chan []byte, sendBuffer),
			closed:    make(chan struct{}),
			accountID: accountID,
		}

		hub.register <- c
		log.Printf("ws connected account_id=%d", accountID)
		go c.writePump()
		go c.readPump(database)

		// Use a background context: the HTTP request context is cancelled once
		// this handler returns, but the connection lives on in the pumps.
		ctx := context.Background()
		if ch, err := database.GetActiveCharacter(ctx, accountID); err == nil {
			c.spawn(ch)
		} else {
			c.safeSend(encode("need_character", MessageData{Message: "create a character to enter the world"}))
		}
	}
}

// spawn adds the character to the world and tells the client it is in.
func (c *Client) spawn(ch db.Character) {
	c.characterID = ch.ID
	c.spawned = true
	x, y := ch.X, ch.Y
	if !c.hub.world.CanWalk(x, y) {
		x, y = c.hub.spawnXY()
	}
	c.hub.world.AddPlayer(ch.ID, ch.Name, x, y, dbSkillsToSet(ch.Skills), ch.TotalXP)
	log.Printf("character spawned account_id=%d character_id=%d name=%q pos=(%.0f,%.0f)",
		c.accountID, ch.ID, ch.Name, x, y)
	skillMap := map[string]int64{}
	if set, total, ok := c.hub.world.PlayerSkillsSnapshot(ch.ID); ok {
		for k, v := range set {
			skillMap[k] = v
		}
		ch.TotalXP = total
	}
	c.safeSend(encode("welcome", WelcomeData{
		CharacterID: ch.ID,
		X:           x,
		Y:           y,
		Name:        ch.Name,
		Skills:      skillMap,
		TotalXp:     ch.TotalXP,
	}))
}

func dbSkillsToSet(m map[string]int64) skills.Set {
	set := skills.NewSet()
	for k, v := range m {
		set[k] = v
	}
	return set
}

func dbFurnitureToDB(items []game.FurnitureItem) []db.FurnitureItem {
	out := make([]db.FurnitureItem, 0, len(items))
	for _, f := range items {
		out = append(out, db.FurnitureItem{Type: f.Type, X: f.X, Y: f.Y})
	}
	return out
}

func (c *Client) readPump(database *db.DB) {
	defer func() {
		if c.spawned {
			if x, y, ok := c.hub.world.Position(c.characterID); ok {
				_ = database.SaveCharacterPosition(context.Background(), c.characterID, x, y)
				if skillSet, total, ok := c.hub.world.SkillsForSave(c.characterID); ok {
					m := map[string]int64{}
					for k, v := range skillSet {
						m[k] = v
					}
					_ = database.SaveCharacterSkills(context.Background(), c.characterID, m, total)
				}
				log.Printf("ws disconnected account_id=%d character_id=%d saved_pos=(%.0f,%.0f)",
					c.accountID, c.characterID, x, y)
			} else {
				log.Printf("ws disconnected account_id=%d character_id=%d", c.accountID, c.characterID)
			}
			c.hub.world.RemovePlayer(c.characterID)
		} else {
			log.Printf("ws disconnected account_id=%d (no character)", c.accountID)
		}
		c.hub.unregister <- c
		c.close()
	}()

	for {
		_, raw, err := c.conn.ReadMessage()
		if err != nil {
			return
		}

		var env Envelope
		if json.Unmarshal(raw, &env) != nil {
			continue
		}

		switch env.Type {
		case "input":
			if !c.spawned {
				continue
			}
			var d InputData
			if json.Unmarshal(env.Data, &d) == nil {
				c.hub.world.SetInput(c.characterID, d.DirX, d.DirY, d.Running)
			}

		case "create_character":
			if c.spawned {
				continue
			}
			var d CreateCharacterData
			if json.Unmarshal(env.Data, &d) != nil {
				continue
			}
			name := strings.TrimSpace(d.Name)
			if name == "" || len(name) > 24 {
				c.safeSend(encode("error", MessageData{Message: "name must be 1-24 characters"}))
				continue
			}
			sx, sy := c.hub.spawnXY()
			ch, err := database.CreateCharacter(context.Background(), c.accountID, name, sx, sy)
			if err != nil {
				log.Printf("character create failed account_id=%d name=%q: %v", c.accountID, name, err)
				c.safeSend(encode("error", MessageData{Message: "could not create character"}))
				continue
			}
			log.Printf("character created account_id=%d character_id=%d name=%q", c.accountID, ch.ID, ch.Name)
			c.spawn(ch)

		case "interact":
			if !c.spawned {
				continue
			}
			var d InteractData
			if json.Unmarshal(env.Data, &d) != nil || d.TargetID == "" {
				continue
			}
			log.Printf("interact account_id=%d character_id=%d target=%q", c.accountID, c.characterID, d.TargetID)
			c.hub.Broadcast(BuildPlayerAction(c.characterID, "interact", 0, 0, d.TargetID))
			if skill, xp, ok := skills.InteractSkill(d.TargetID); ok {
				now := float64(time.Now().UnixMilli()) / 1000
				if c.hub.world.CanInteractSkill(c.characterID, now, 2.0) {
					if r, granted := c.hub.world.GrantSkillXP(c.characterID, skill, xp); granted {
						c.sendSkillXpGain(c.characterID, skill, xp, r)
					}
				}
			}

		case "player_action":
			if !c.spawned {
				continue
			}
			var d PlayerActionSendData
			if json.Unmarshal(env.Data, &d) != nil || d.Action == "" {
				continue
			}
			dirX, dirY := normalizeDir(d.DirX, d.DirY)
			c.hub.Broadcast(BuildPlayerAction(c.characterID, d.Action, dirX, dirY, d.TargetID))

		case "cast_fireball":
			if !c.spawned {
				continue
			}
			var d CastFireballData
			if json.Unmarshal(env.Data, &d) != nil {
				continue
			}
			c.broadcastProjectileCast(c.characterID, "fireball", d.DirX, d.DirY)

		case "cast_spell":
			if !c.spawned {
				continue
			}
			var d CastSpellData
			if json.Unmarshal(env.Data, &d) != nil || d.SpellID == "" {
				continue
			}
			dirX, dirY := normalizeDir(d.DirX, d.DirY)
			dirX, dirY = cardinalDir(dirX, dirY)
			switch d.SpellID {
			case "fireball", "ice_shard":
				c.broadcastProjectileCast(c.characterID, d.SpellID, dirX, dirY)
			case "arc_bolt":
				c.hub.Broadcast(BuildPlayerAction(c.characterID, "cast_arc_bolt", dirX, dirY, ""))
			case "blood_bolt":
				c.hub.Broadcast(BuildPlayerAction(c.characterID, "cast_blood_bolt", dirX, dirY, ""))
			case "poison_cloud":
				x, y, ok := c.hub.world.Position(c.characterID)
				if !ok {
					continue
				}
				c.hub.Broadcast(encode("spell_effect_spawn", SpellEffectSpawnData{
					OwnerID: c.characterID,
					SpellID: d.SpellID,
					X:       x,
					Y:       y,
					DirX:    dirX,
					DirY:    dirY,
				}))
				c.hub.Broadcast(BuildPlayerAction(c.characterID, "cast_poison_cloud", dirX, dirY, ""))
			case "shield_bash":
				c.hub.Broadcast(BuildPlayerAction(c.characterID, "shield_bash", dirX, dirY, ""))
			case "whirlwind":
				c.hub.Broadcast(BuildPlayerAction(c.characterID, "whirlwind", dirX, dirY, ""))
			case "warrior_dash":
				c.hub.world.DashPlayer(c.characterID, dirX, dirY, game.WarriorDashRange)
				c.hub.Broadcast(BuildPlayerAction(c.characterID, "warrior_dash", dirX, dirY, ""))
			case "hunter_mark":
				markTarget := c.hub.world.NearestEnemyInCone(c.characterID, dirX, dirY, game.HunterMarkRange, 0.25)
				if markTarget <= 0 {
					continue
				}
				ev, ok := c.hub.world.ApplyPlayerBuff(c.characterID, game.BuffHunterMark, markTarget)
				if !ok {
					continue
				}
				c.hub.Broadcast(BuildPlayerBuff(ev.PlayerID, ev.BuffID, ev.Duration, ev.MarkTargetID))
				c.hub.Broadcast(BuildPlayerAction(c.characterID, "hunter_mark", dirX, dirY, formatTargetID(markTarget)))
			default:
				continue
			}

		case "chat_message":
			if !c.spawned {
				continue
			}
			var d ChatMessageSendData
			if json.Unmarshal(env.Data, &d) != nil {
				continue
			}
			text := strings.TrimSpace(d.Text)
			if text == "" || len(text) > 120 {
				continue
			}
			c.hub.Broadcast(encode("chat_message", ChatMessageData{
				PlayerID: c.characterID,
				Text:     text,
			}))

		case "chat_typing":
			if !c.spawned {
				continue
			}
			var d ChatTypingSendData
			if json.Unmarshal(env.Data, &d) != nil {
				continue
			}
			c.hub.Broadcast(encode("chat_typing", ChatTypingData{
				PlayerID: c.characterID,
				Typing:   d.Typing,
			}))

		case "ability_hit":
			if !c.spawned {
				continue
			}
			var d AbilityHitSendData
			if json.Unmarshal(env.Data, &d) != nil || d.TargetID <= 0 || d.Ability == "" {
				continue
			}
			if d.TargetID == c.characterID {
				continue
			}
			damage := game.DamageForAbility(d.Ability)
			if damage <= 0 {
				continue
			}
			if _, _, ok := c.hub.world.Position(d.TargetID); !ok {
				continue
			}
			if !c.hub.world.ValidateAbilityHit(c.characterID, d.TargetID, d.Ability) {
				continue
			}
			if !c.hub.world.PvPAllowedBetween(c.characterID, d.TargetID) {
				continue
			}
			damage = int(float64(damage) * c.hub.world.DamageDealtMultiplier(c.characterID))
			damage = int(float64(damage) * c.hub.world.DamageTakenMultiplier(d.TargetID))
			if damage <= 0 {
				continue
			}
			hp, hpMax, justDied, ok := c.hub.world.ApplyDamage(d.TargetID, damage)
			if !ok {
				continue
			}
			c.hub.Broadcast(encode("player_hit", PlayerHitData{
				AttackerID: c.characterID,
				TargetID:   d.TargetID,
				Damage:     damage,
				Ability:    d.Ability,
				Hp:         hp,
				HpMax:      hpMax,
			}))
			if justDied {
				c.hub.HandlePlayerDeath(database, d.TargetID, c.characterID)
			}
			c.grantCombatSkillXP(c.characterID, d.TargetID, d.Ability, damage)

		case "ability_hit_npc":
			if !c.spawned {
				continue
			}
			var d AbilityHitNpcSendData
			if json.Unmarshal(env.Data, &d) != nil || d.TargetNpcID >= 0 || d.Ability == "" {
				continue
			}
			damage := game.DamageForAbility(d.Ability)
			if damage <= 0 {
				continue
			}
			if _, _, ok := c.hub.world.NpcPosition(d.TargetNpcID); !ok {
				continue
			}
			if !c.hub.world.ValidateNpcHit(c.characterID, d.TargetNpcID, d.Ability) {
				continue
			}
			damage = int(float64(damage) * c.hub.world.DamageDealtMultiplier(c.characterID))
			if damage <= 0 {
				continue
			}
			hp, hpMax, justDied, ok := c.hub.world.ApplyDamageToNpc(d.TargetNpcID, damage)
			if !ok {
				continue
			}
			c.hub.Broadcast(BuildNpcHit(c.characterID, d.TargetNpcID, damage, d.Ability, hp, hpMax))
			c.hub.ProcessBossEvents(c.hub.world.DrainPendingBossEvents())
			if justDied {
				// Boss death events already queued in ApplyDamageToNpc.
				_ = justDied
			}

		case "ability_use":
			if !c.spawned {
				continue
			}
			var d AbilityUseSendData
			if json.Unmarshal(env.Data, &d) != nil || d.Ability == "" {
				continue
			}
			if d.Ability == "bandage" {
				if !c.hub.world.StartBandageHoT(c.characterID) {
					continue
				}
				c.hub.Broadcast(BuildPlayerAction(c.characterID, "use_bandage", 0, 0, ""))
				continue
			}
			switch d.Ability {
			case game.BuffBattleShout, game.BuffIronSkin:
				ev, ok := c.hub.world.ApplyPlayerBuff(c.characterID, d.Ability, 0)
				if !ok {
					continue
				}
				c.hub.Broadcast(BuildPlayerBuff(ev.PlayerID, ev.BuffID, ev.Duration, ev.MarkTargetID))
				action := "battle_shout"
				if d.Ability == game.BuffIronSkin {
					action = "iron_skin"
				}
				c.hub.Broadcast(BuildPlayerAction(c.characterID, action, 0, 0, ""))
				continue
			}
			amount := game.HealForAbility(d.Ability)
			if amount <= 0 {
				continue
			}
			hp, hpMax, ok := c.hub.world.ApplyHeal(c.characterID, amount)
			if !ok {
				continue
			}
			c.hub.Broadcast(encode("player_heal", PlayerHealData{
				PlayerID: c.characterID,
				Amount:   amount,
				Ability:  d.Ability,
				Hp:       hp,
				HpMax:    hpMax,
			}))
			if d.Ability == "second_wind" {
				c.hub.Broadcast(BuildPlayerAction(c.characterID, "second_wind", 0, 0, ""))
			}

		case "build_house":
			if !c.spawned {
				continue
			}
			var d BuildHouseSendData
			_ = json.Unmarshal(env.Data, &d)
			px, py, ok := c.hub.world.Position(c.characterID)
			if !ok {
				continue
			}
			if d.X != nil {
				px = *d.X
			}
			if d.Y != nil {
				py = *d.Y
			}
			name, _ := c.hub.world.PlayerName(c.characterID)
			if _, msg, ok := c.hub.world.BuildHouse(c.characterID, name, px, py); !ok {
				c.safeSend(encode("error", MessageData{Message: msg}))
				continue
			}
			house, err := database.CreateHouse(context.Background(), c.characterID, name, px, py)
			if err != nil {
				if errors.Is(err, db.ErrHouseExists) {
					c.safeSend(encode("error", MessageData{Message: "You already have a house."}))
				} else {
					c.safeSend(encode("error", MessageData{Message: "Could not build house."}))
				}
				continue
			}
			state := c.hub.world.RegisterHouse(house)
			c.hub.Broadcast(BuildHouseBuilt(state))

		case "place_furniture":
			if !c.spawned {
				continue
			}
			var d PlaceFurnitureSendData
			if json.Unmarshal(env.Data, &d) != nil || d.Type == "" {
				continue
			}
			item := game.FurnitureItem{Type: d.Type, X: d.X, Y: d.Y}
			state, msg, ok := c.hub.world.PlaceFurniture(c.characterID, item)
			if !ok {
				c.safeSend(encode("error", MessageData{Message: msg}))
				continue
			}
			if items, ok := c.hub.world.FurnitureForSave(c.characterID); ok {
				_ = database.SaveHouseFurniture(context.Background(), c.characterID, dbFurnitureToDB(items))
			}
			c.hub.Broadcast(BuildHouseUpdated(state))

		case "logout":
			if !c.spawned {
				continue
			}
			if x, y, ok := c.hub.world.Position(c.characterID); ok {
				_ = database.SaveCharacterPosition(context.Background(), c.characterID, x, y)
				log.Printf("logout save account_id=%d character_id=%d pos=(%.0f,%.0f)",
					c.accountID, c.characterID, x, y)
			}
			return
		}
	}
}

func cardinalDir(dirX, dirY float64) (float64, float64) {
	if math.Abs(dirX) > math.Abs(dirY) {
		if dirX >= 0 {
			return 1, 0
		}
		return -1, 0
	}
	if dirY >= 0 {
		return 0, 1
	}
	return 0, -1
}

func projectileSpawnPoint(x, y, dirX, dirY float64) (float64, float64) {
	const playerRadius = 12.0
	const torsoYOffset = -11.0
	const spawnOffset = 8.0
	ox := x + dirX*(playerRadius+spawnOffset)
	oy := y + torsoYOffset + dirY*(playerRadius+spawnOffset)
	return ox, oy
}

func normalizeDir(dirX, dirY float64) (float64, float64) {
	if l := math.Hypot(dirX, dirY); l > 0.01 {
		return dirX / l, dirY / l
	}
	return 0, 1
}

func formatTargetID(id int64) string {
	return strconv.FormatInt(id, 10)
}

func (c *Client) broadcastProjectileCast(playerID int64, spellID string, dirX, dirY float64) {
	x, y, ok := c.hub.world.Position(playerID)
	if !ok {
		return
	}
	dirX, dirY = normalizeDir(dirX, dirY)
	dirX, dirY = cardinalDir(dirX, dirY)
	ox, oy := projectileSpawnPoint(x, y, dirX, dirY)
	c.hub.Broadcast(encode("projectile_spawn", ProjectileSpawnData{
		OwnerID: playerID,
		SpellID: spellID,
		X:       ox,
		Y:       oy,
		DirX:    dirX,
		DirY:    dirY,
	}))
	action := "cast_fireball"
	if spellID == "ice_shard" {
		action = "cast_ice_shard"
	}
	c.hub.Broadcast(BuildPlayerAction(playerID, action, dirX, dirY, ""))
}

func (c *Client) sendSkillXpGain(playerID int64, skill string, amount int64, r game.SkillGrantResult) {
	_, total, _ := c.hub.world.PlayerSkillsSnapshot(playerID)
	msg := SkillXpGainData{
		PlayerID:  playerID,
		SkillID:   skill,
		Amount:    amount,
		Xp:        r.Xp,
		Level:     r.Level,
		LeveledUp: r.LeveledUp,
		TotalXp:   total,
	}
	if skill == skills.Hitpoints {
		if hp, hpMax, ok := c.hub.world.PlayerHP(playerID); ok {
			msg.Hp = hp
			msg.HpMax = hpMax
		}
	}
	payload := BuildSkillXpGain(msg)
	if playerID == c.characterID {
		c.safeSend(payload)
	} else {
		c.hub.SendToCharacter(playerID, payload, false)
	}
}

func (c *Client) grantCombatSkillXP(attackerID, defenderID int64, ability string, damage int) {
	att, def := skills.CombatXP(ability, damage)
	for skill, amt := range att {
		if r, ok := c.hub.world.GrantSkillXP(attackerID, skill, amt); ok {
			c.sendSkillXpGain(attackerID, skill, amt, r)
		}
	}
	for skill, amt := range def {
		if r, ok := c.hub.world.GrantSkillXP(defenderID, skill, amt); ok {
			c.sendSkillXpGain(defenderID, skill, amt, r)
		}
	}
}

func (c *Client) writePump() {
	for {
		select {
		case msg := <-c.send:
			_ = c.conn.SetWriteDeadline(time.Now().Add(writeTimeout))
			if err := c.conn.WriteMessage(websocket.TextMessage, msg); err != nil {
				c.close()
				return
			}
		case <-c.closed:
			return
		}
	}
}
