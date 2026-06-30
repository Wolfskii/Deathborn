package net

import (
	"context"

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

// HandlePlayerDeath removes a player from the world and notifies clients.
func (h *Hub) HandlePlayerDeath(database *db.DB, playerID, killerID int64) {
	x, y, dirX, dirY, ok := h.world.DeathPose(playerID)
	if !ok {
		return
	}
	_ = database.MarkCharacterDead(context.Background(), playerID)
	h.Broadcast(BuildPlayerDeath(playerID, killerID, x, y, dirX, dirY))
	h.SendToCharacter(playerID, BuildYouDied(x, y, dirX, dirY), true)
	h.world.RemovePlayer(playerID)
}

// spawnXY is the default position for newly created characters on Realik.
func (h *Hub) spawnXY() (float64, float64) {
	if h.world != nil {
		return h.world.DefaultSpawn()
	}
	return 0, 0
}
