package net

import (
	"context"
	"log"
	"sync"
	"time"

	"github.com/deathborn/server/internal/db"
	"github.com/deathborn/server/internal/game"
)

// Hub owns the set of connected clients and fans out broadcasts. Register,
// unregister, and broadcast all flow through a single goroutine (Run) so the
// clients map needs no extra locking.
type Hub struct {
	world *game.World
	db    *db.DB

	register   chan *Client
	unregister chan *Client
	broadcast  chan []byte
	directSend chan directMessage
	clients    map[*Client]bool

	onlineMu      sync.RWMutex
	onlineAccount map[int64]int64

	shuttingDown bool
}

type directMessage struct {
	characterID   int64
	payload       []byte
	markUnspawned bool
}

func NewHub(world *game.World, database *db.DB) *Hub {
	return &Hub{
		world:      world,
		db:         database,
		register:   make(chan *Client),
		unregister: make(chan *Client),
		broadcast:  make(chan []byte, 8),
		directSend: make(chan directMessage, 8),
		clients:    make(map[*Client]bool),
	}
}

// Run processes hub events until the context is cancelled.
func (h *Hub) Run(ctx context.Context) {
	for {
		select {
		case <-ctx.Done():
			h.gracefulShutdownAll("The realm is closing for maintenance. Your progress has been saved — please log in again shortly.")
			return
		case c := <-h.register:
			h.clients[c] = true
		case c := <-h.unregister:
			delete(h.clients, c)
		case msg := <-h.broadcast:
			for c := range h.clients {
				if !c.trySend(msg) {
					// Slow client: drop it rather than blocking the hub.
					delete(h.clients, c)
					c.close()
				}
			}
		case dm := <-h.directSend:
			for c := range h.clients {
				if c.characterID == dm.characterID {
					if dm.markUnspawned {
						c.markUnspawned()
					}
					c.safeSend(dm.payload)
					break
				}
			}
		}
	}
}

// Broadcast queues a message for delivery to all clients.
func (h *Hub) Broadcast(msg []byte) {
	h.broadcast <- msg
}

// SendToCharacter queues a message for one connected character.
func (h *Hub) SendToCharacter(characterID int64, msg []byte, markUnspawned bool) {
	if characterID <= 0 {
		return
	}
	h.directSend <- directMessage{characterID: characterID, payload: msg, markUnspawned: markUnspawned}
}

func (h *Hub) SetOnline(accountID, characterID int64) {
	if accountID <= 0 || characterID <= 0 {
		return
	}
	h.onlineMu.Lock()
	if h.onlineAccount == nil {
		h.onlineAccount = make(map[int64]int64)
	}
	h.onlineAccount[accountID] = characterID
	h.onlineMu.Unlock()
}

func (h *Hub) ClearOnline(accountID int64) {
	if accountID <= 0 {
		return
	}
	h.onlineMu.Lock()
	delete(h.onlineAccount, accountID)
	h.onlineMu.Unlock()
}

func (h *Hub) OnlineCharacter(accountID int64) (int64, bool) {
	h.onlineMu.RLock()
	defer h.onlineMu.RUnlock()
	if h.onlineAccount == nil {
		return 0, false
	}
	id, ok := h.onlineAccount[accountID]
	return id, ok
}

// ProcessBossEvents broadcasts boss lifecycle and combat events.
func (h *Hub) ProcessBossEvents(events []game.BossEvent) {
	for _, ev := range events {
		switch ev.Type {
		case "spawn":
			h.Broadcast(BuildBossSpawn(ev.NpcID, ev.DefID, ev.Name, ev.X, ev.Y))
		case "death":
			h.Broadcast(BuildBossDeath(ev.NpcID, ev.DefID, ev.Name, ev.X, ev.Y))
		case "world_event":
			h.Broadcast(BuildWorldEvent(WorldEventData{
				Active: ev.Active, Name: ev.Name, PvPOff: ev.Active, BossCnt: ev.BossCnt,
			}))
		case "player_hit":
			ability := ev.Name
			if ability == "" {
				ability = "boss_attack"
			}
			h.Broadcast(encode("player_hit", PlayerHitData{
				AttackerID: ev.NpcID,
				TargetID:   ev.PlayerID,
				Damage:     ev.Damage,
				Ability:    ability,
				Hp:         ev.Hp,
				HpMax:      ev.HpMax,
			}))
			if ev.JustDied {
				h.HandlePlayerDeath(h.db, ev.PlayerID, ev.NpcID)
			}
		case "boss_action":
			x, y, _ := h.world.NpcPosition(ev.NpcID)
			h.Broadcast(BuildBossAction(ev.NpcID, ev.Name, x, y))
		}
	}
}

// ProcessExpiredWorldDrops despawns ground loot past its lifetime.
func (h *Hub) ProcessExpiredWorldDrops() {
	ctx := context.Background()
	for _, dropID := range h.world.PruneExpiredDrops(time.Now()) {
		_ = h.db.DeleteWorldItemDrop(ctx, dropID)
		h.Broadcast(BuildWorldItemRemoved(dropID))
	}
}

// HandlePlayerDeath removes a player from the world and notifies clients.
func (h *Hub) HandlePlayerDeath(database *db.DB, playerID, killerID int64) {
	x, y, dirX, dirY, ok := h.world.DeathPose(playerID)
	if !ok {
		return
	}
	ctx := context.Background()
	h.dropInventoryOnDeath(ctx, database, playerID, x, y)
	_ = database.MarkCharacterDead(ctx, playerID)
	h.Broadcast(BuildPlayerDeath(playerID, killerID, x, y, dirX, dirY))
	h.SendToCharacter(playerID, BuildYouDied(x, y, dirX, dirY), true)
	h.world.RemovePlayer(playerID)
}

func (h *Hub) gracefulShutdownAll(reason string) {
	if h.shuttingDown {
		return
	}
	h.shuttingDown = true
	if reason == "" {
		reason = "The server is restarting. Please log in again shortly."
	}

	clients := make([]*Client, 0, len(h.clients))
	for c := range h.clients {
		clients = append(clients, c)
	}
	if len(clients) == 0 {
		log.Println("graceful shutdown: no connected clients")
		return
	}

	log.Printf("graceful shutdown: notifying %d connected clients", len(clients))
	msg := BuildServerShutdown(reason)
	for _, c := range clients {
		c.safeSend(msg)
	}
	time.Sleep(150 * time.Millisecond)

	for _, c := range clients {
		c.endSession(h.db, false)
		delete(h.clients, c)
		c.close()
	}
	log.Println("graceful shutdown complete")
}

// PersistDirtyFarms writes homestead crop/animal blobs that changed since last save.
func (h *Hub) PersistDirtyFarms() {
	if h.world == nil || h.db == nil {
		return
	}
	ctx := context.Background()
	for _, row := range h.world.TakeDirtyFarms() {
		if err := h.db.SaveHouseFarm(ctx, row.HouseID, row.Farm); err != nil {
			log.Printf("save house farm %d: %v", row.HouseID, err)
		}
	}
}

// PersistDirtyFurniture writes starter kitchens added on house load/build.
func (h *Hub) PersistDirtyFurniture() {
	if h.world == nil || h.db == nil {
		return
	}
	ctx := context.Background()
	for _, row := range h.world.TakeDirtyFurniture() {
		if err := h.db.SaveHouseFurniture(ctx, row.CharacterID, dbFurnitureToDB(row.Items)); err != nil {
			log.Printf("save house furniture character=%d: %v", row.CharacterID, err)
		}
	}
}

// spawnXY is the default position for newly created characters on Realik.
func (h *Hub) spawnXY() (float64, float64) {
	if h.world != nil {
		return h.world.DefaultSpawn()
	}
	return 0, 0
}
