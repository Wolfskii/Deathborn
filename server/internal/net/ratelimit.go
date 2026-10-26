package net

import "time"

type bucket struct {
	tokens, capacity, refillPerSec float64
	last                           time.Time
}

func newBucket(capacity, refillPerSec float64, now time.Time) *bucket {
	return &bucket{tokens: capacity, capacity: capacity, refillPerSec: refillPerSec, last: now}
}

// allow refills by elapsed time, then spends one token if available.
func (b *bucket) allow(now time.Time) bool {
	if dt := now.Sub(b.last).Seconds(); dt > 0 {
		b.tokens += dt * b.refillPerSec
		if b.tokens > b.capacity {
			b.tokens = b.capacity
		}
		b.last = now
	}
	if b.tokens < 1 {
		return false
	}
	b.tokens--
	return true
}

type limitSpec struct{ capacity, refillPerSec float64 }

// limitSpecs are per message type; anything missing uses defaultLimit.
var limitSpecs = map[string]limitSpec{
	"input":           {120, 60},
	"chat_message":    {4, 0.5},
	"chat_typing":     {8, 4},
	"pm_send":         {3, 0.5},
	"friend_add":      {5, 1},
	"friend_respond":  {5, 1},
	"friend_remove":   {5, 1},
	"ability_hit":     {20, 10},
	"ability_hit_npc": {20, 10},
	"ability_use":     {20, 10},
}

var defaultLimit = limitSpec{40, 20}

// limiterSet is owned by one client's readPump goroutine, so no locking.
type limiterSet struct{ m map[string]*bucket }

func newLimiterSet() *limiterSet { return &limiterSet{m: map[string]*bucket{}} }

func (s *limiterSet) allow(msgType string, now time.Time) bool {
	b, ok := s.m[msgType]
	if !ok {
		spec, found := limitSpecs[msgType]
		if !found {
			spec = defaultLimit
		}
		b = newBucket(spec.capacity, spec.refillPerSec, now)
		s.m[msgType] = b
	}
	return b.allow(now)
}
