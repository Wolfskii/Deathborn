package game

import (
	"context"
	"time"
)

// RunLoop drives the simulation at a fixed tick rate. Each tick it advances the
// world and then invokes onTick, which the caller uses to build and broadcast a
// snapshot. The networking/serialization concern is kept out of this package.
func RunLoop(ctx context.Context, w *World, tickHz int, onTick func(tick uint64, heals []HealEvent)) {
	if tickHz <= 0 {
		tickHz = 20
	}
	interval := time.Second / time.Duration(tickHz)
	dt := interval.Seconds()

	ticker := time.NewTicker(interval)
	defer ticker.Stop()

	var tick uint64
	for {
		select {
		case <-ctx.Done():
			return
		case <-ticker.C:
			tick++
			heals := w.Step(dt)
			onTick(tick, heals)
		}
	}
}
