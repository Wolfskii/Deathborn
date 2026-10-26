package auth

import (
	"net/http/httptest"
	"testing"
	"time"
)

func TestIPLimiterAllowsMaxThenRejects(t *testing.T) {
	l := newIPLimiter(10, time.Minute)
	now := time.Unix(1000, 0)
	for i := 0; i < 10; i++ {
		if !l.Allow("1.2.3.4", now) {
			t.Fatalf("attempt %d should be allowed", i+1)
		}
	}
	if l.Allow("1.2.3.4", now) {
		t.Fatal("11th attempt should be rejected")
	}
	if !l.Allow("5.6.7.8", now) {
		t.Fatal("a different IP has its own budget")
	}
}

func TestIPLimiterWindowResets(t *testing.T) {
	l := newIPLimiter(2, time.Minute)
	now := time.Unix(1000, 0)
	l.Allow("ip", now)
	l.Allow("ip", now)
	if l.Allow("ip", now) {
		t.Fatal("third attempt inside the window should be rejected")
	}
	if !l.Allow("ip", now.Add(61*time.Second)) {
		t.Fatal("attempt after the window should be allowed")
	}
}

func TestClientIPIgnoresForwardedHeaderUnlessTrusted(t *testing.T) {
	r := httptest.NewRequest("POST", "/login", nil)
	r.RemoteAddr = "10.0.0.1:5555"
	r.Header.Set("X-Forwarded-For", "203.0.113.9, 10.0.0.2")

	if got := clientIP(r, false); got != "10.0.0.1" {
		t.Fatalf("untrusted proxy: got %q, want 10.0.0.1", got)
	}
	if got := clientIP(r, true); got != "203.0.113.9" {
		t.Fatalf("trusted proxy: got %q, want 203.0.113.9", got)
	}
}
