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
	clients    map[*Client]bool
}

func NewHub(world *game.World, database *db.DB) *Hub {
	return &Hub{
		world:      world,
		db:         database,
		register:   make(chan *Client),
		unregister: make(chan *Client),
		broadcast:  make(chan []byte, 8),
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
		}
	}
}

// Broadcast queues a message for delivery to all clients.
func (h *Hub) Broadcast(msg []byte) {
	h.broadcast <- msg
}
