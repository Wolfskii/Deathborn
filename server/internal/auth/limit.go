package auth

import (
	"net"
	"net/http"
	"strings"
	"sync"
	"time"
)

// ipLimiter allows at most max attempts per IP within window.
type ipLimiter struct {
	mu     sync.Mutex
	hits   map[string][]time.Time
	max    int
	window time.Duration
}

func newIPLimiter(max int, window time.Duration) *ipLimiter {
	return &ipLimiter{hits: map[string][]time.Time{}, max: max, window: window}
}

// Allow records an attempt from ip and reports whether it is within the limit.
func (l *ipLimiter) Allow(ip string, now time.Time) bool {
	l.mu.Lock()
	defer l.mu.Unlock()

	if len(l.hits) > 10000 {
		l.pruneLocked(now)
	}

	recent := l.recentLocked(ip, now)
	if len(recent) >= l.max {
		l.hits[ip] = recent
		return false
	}
	l.hits[ip] = append(recent, now)
	return true
}

// recentLocked returns the timestamps for ip that are still inside the window.
func (l *ipLimiter) recentLocked(ip string, now time.Time) []time.Time {
	cutoff := now.Add(-l.window)
	kept := l.hits[ip][:0]
	for _, t := range l.hits[ip] {
		if t.After(cutoff) {
			kept = append(kept, t)
		}
	}
	return kept
}

// pruneLocked drops IPs that have no attempts inside the window.
func (l *ipLimiter) pruneLocked(now time.Time) {
	for ip := range l.hits {
		if len(l.recentLocked(ip, now)) == 0 {
			delete(l.hits, ip)
		}
	}
}

// clientIP returns the caller's IP. X-Forwarded-For is only trusted when the
// server runs behind a proxy that sets it (TRUST_PROXY=true).
func clientIP(r *http.Request, trustProxy bool) string {
	if trustProxy {
		if fwd := r.Header.Get("X-Forwarded-For"); fwd != "" {
			first := strings.TrimSpace(strings.Split(fwd, ",")[0])
			if first != "" {
				return first
			}
		}
	}
	host, _, err := net.SplitHostPort(r.RemoteAddr)
	if err != nil {
		return r.RemoteAddr
	}
	return host
}
