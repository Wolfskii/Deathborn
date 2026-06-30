// Command deathborn is the authoritative game server: HTTP auth endpoints, a
// WebSocket world connection, and a fixed-tick simulation, all in one process.
package main

import (
	"context"
	"log"
	"net/http"
	"os"
	"os/signal"
	"syscall"
	"time"

	"github.com/deathborn/server/internal/auth"
	"github.com/deathborn/server/internal/config"
	"github.com/deathborn/server/internal/db"
	"github.com/deathborn/server/internal/game"
	gnet "github.com/deathborn/server/internal/net"
	"github.com/deathborn/server/internal/worldmap"
	"github.com/deathborn/server/migrations"
)

const tickHz = 20

// version is set at build time via -ldflags "-X main.version=...". It defaults
// to "dev" for local `go run`.
var version = "dev"

func main() {
	cfg := config.Load()
	log.Printf("deathborn %s starting", version)

	ctx, stop := signal.NotifyContext(context.Background(), os.Interrupt, syscall.SIGTERM)
	defer stop()

	pool, err := db.Connect(ctx, cfg.DatabaseURL)
	if err != nil {
		log.Fatalf("connect database: %v", err)
	}
	defer pool.Close()

	if err := db.RunMigrations(ctx, pool, migrations.Files); err != nil {
		log.Fatalf("run migrations: %v", err)
	}
	log.Println("migrations applied")

	database := db.New(pool)
	terrain, err := worldmap.LoadEmbedded()
	if err != nil {
		log.Fatalf("load world map: %v", err)
	}
	log.Printf("world map loaded %dx%d tiles (%.0fx%.0f world units) spawn=(%.0f,%.0f)",
		terrain.TileWidth, terrain.TileHeight, terrain.WorldWidth, terrain.WorldHeight,
		terrain.DefaultSpawnX, terrain.DefaultSpawnY)

	world := game.NewWorld(terrain)
	if houses, err := database.ListHouses(ctx); err != nil {
		log.Fatalf("load houses: %v", err)
	} else {
		world.LoadHouses(houses)
		log.Printf("loaded %d player houses", len(houses))
	}
	if drops, err := database.ListWorldItemDrops(ctx); err != nil {
		log.Fatalf("load world item drops: %v", err)
	} else {
		world.LoadWorldDrops(drops)
		log.Printf("loaded %d world item drops", len(drops))
	}
	hub := gnet.NewHub(world, database)
	go hub.Run(ctx)

	// Simulation loop: advance the world and broadcast a snapshot each tick.
	go game.RunLoop(ctx, world, tickHz, func(tick uint64, heals []game.HealEvent, dt float64) {
		hub.ProcessBossEvents(world.TickBosses(dt))
		hub.ProcessBossEvents(world.DrainPendingBossEvents())
		for _, h := range heals {
			hub.Broadcast(gnet.BuildPlayerHeal(h.PlayerID, h.Amount, h.Ability, h.Hp, h.HpMax))
		}
		for _, b := range world.TickBuffs(dt) {
			hub.Broadcast(gnet.BuildPlayerBuff(b.PlayerID, b.BuffID, b.Duration, b.MarkTargetID))
		}
		hub.Broadcast(gnet.BuildSnapshot(tick, world.Snapshot(), world.NpcSnapshot(), world.HouseSnapshot(), world.DropSnapshot(), world.WorldEventSnapshot()))
	})

	authH := auth.NewHandler(database, cfg.JWTSecret)

	mux := http.NewServeMux()
	mux.HandleFunc("/register", authH.Register)
	mux.HandleFunc("/login", authH.Login)
	mux.HandleFunc("/ws", gnet.ServeWS(hub, database, cfg.JWTSecret))
	mux.HandleFunc("/health", func(w http.ResponseWriter, r *http.Request) {
		w.WriteHeader(http.StatusOK)
		_, _ = w.Write([]byte("ok"))
	})

	srv := &http.Server{
		Addr:    cfg.HTTPAddr(),
		Handler: gnet.LogRequests(mux),
	}

	go func() {
		log.Printf("deathborn server listening on %s (tick %dHz)", cfg.HTTPAddr(), tickHz)
		if err := srv.ListenAndServe(); err != nil && err != http.ErrServerClosed {
			log.Fatalf("http server: %v", err)
		}
	}()

	<-ctx.Done()
	log.Println("shutting down")

	shutdownCtx, cancel := context.WithTimeout(context.Background(), 5*time.Second)
	defer cancel()
	_ = srv.Shutdown(shutdownCtx)
}
