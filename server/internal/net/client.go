package net

import (
	"context"
	"encoding/json"
	"log"
	"math"
	"net/http"
	"strings"
	"sync"
	"time"

	"github.com/deathborn/server/internal/auth"
	"github.com/deathborn/server/internal/db"
	"github.com/gorilla/websocket"
)

// Spawn point for newly created characters (a "Starter Town" placeholder).
const (
	spawnX = 0.0
	spawnY = 0.0

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
	c.hub.world.AddPlayer(ch.ID, ch.Name, ch.X, ch.Y)
	log.Printf("character spawned account_id=%d character_id=%d name=%q pos=(%.0f,%.0f)",
		c.accountID, ch.ID, ch.Name, ch.X, ch.Y)
	c.safeSend(encode("welcome", WelcomeData{
		CharacterID: ch.ID,
		X:           ch.X,
		Y:           ch.Y,
		Name:        ch.Name,
	}))
}

func (c *Client) readPump(database *db.DB) {
	defer func() {
		if c.spawned {
			if x, y, ok := c.hub.world.Position(c.characterID); ok {
				_ = database.SaveCharacterPosition(context.Background(), c.characterID, x, y)
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
				c.hub.world.SetInput(c.characterID, d.DirX, d.DirY)
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
			ch, err := database.CreateCharacter(context.Background(), c.accountID, name, spawnX, spawnY)
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
			// Range validation and gameplay effects come in later milestones.

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
			x, y, ok := c.hub.world.Position(c.characterID)
			if !ok {
				continue
			}
			dirX, dirY := normalizeDir(d.DirX, d.DirY)
			dirX, dirY = cardinalDir(dirX, dirY)
			ox, oy := projectileSpawnPoint(x, y, dirX, dirY)
			c.hub.Broadcast(encode("projectile_spawn", ProjectileSpawnData{
				OwnerID: c.characterID,
				X:       ox,
				Y:       oy,
				DirX:    dirX,
				DirY:    dirY,
			}))
			c.hub.Broadcast(BuildPlayerAction(c.characterID, "cast_fireball", dirX, dirY, ""))

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
			if json.Unmarshal(env.Data, &d) != nil || d.TargetID <= 0 || d.Damage <= 0 || d.Ability == "" {
				continue
			}
			if _, _, ok := c.hub.world.Position(d.TargetID); !ok {
				continue
			}
			c.hub.Broadcast(encode("player_hit", PlayerHitData{
				AttackerID: c.characterID,
				TargetID:   d.TargetID,
				Damage:     d.Damage,
				Ability:    d.Ability,
			}))

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
