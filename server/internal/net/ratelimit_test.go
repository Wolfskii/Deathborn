package net

import (
	"testing"
	"time"
)

func TestBucketRejectsWhenEmpty(t *testing.T) {
	now := time.Unix(1000, 0)
	b := newBucket(3, 1, now)
	for i := 0; i < 3; i++ {
		if !b.allow(now) {
			t.Fatalf("call %d should be allowed", i+1)
		}
	}
	if b.allow(now) {
		t.Fatal("4th call at the same instant should be rejected")
	}
}

func TestBucketRefillsOverTime(t *testing.T) {
	now := time.Unix(1000, 0)
	b := newBucket(3, 1, now)
	for i := 0; i < 3; i++ {
		b.allow(now)
	}
	later := now.Add(2 * time.Second)
	if !b.allow(later) || !b.allow(later) {
		t.Fatal("two tokens should have refilled after 2s")
	}
	if b.allow(later) {
		t.Fatal("third call after 2s should be rejected")
	}
}

func TestLimiterSetKeepsTypesSeparate(t *testing.T) {
	now := time.Unix(1000, 0)
	s := newLimiterSet()
	for i := 0; i < 4; i++ {
		s.allow("chat_message", now) // capacity 4, so these use it up
	}
	if s.allow("chat_message", now) {
		t.Fatal("chat_message should be exhausted")
	}
	if !s.allow("input", now) {
		t.Fatal("input has its own bucket and should still be allowed")
	}
}

func TestLimiterSetUsesDefaultForUnknownType(t *testing.T) {
	now := time.Unix(1000, 0)
	s := newLimiterSet()
	for i := 0; i < int(defaultLimit.capacity); i++ {
		if !s.allow("some_new_type", now) {
			t.Fatalf("call %d should be allowed under default limit", i+1)
		}
	}
	if s.allow("some_new_type", now) {
		t.Fatal("unknown type should be capped at the default capacity")
	}
}
