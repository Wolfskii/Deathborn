package net

import (
	"bufio"
	"fmt"
	"log"
	"net"
	"net/http"
	"time"
)

// statusRecorder captures the HTTP status code for request logging. It delegates
// http.Hijacker (and Flusher) so WebSocket upgrades still work through this
// middleware — wrapping ResponseWriter without Hijacker is a common cause of
// GET /ws -> 500.
type statusRecorder struct {
	http.ResponseWriter
	status int
}

func (r *statusRecorder) WriteHeader(code int) {
	r.status = code
	r.ResponseWriter.WriteHeader(code)
}

func (r *statusRecorder) Hijack() (net.Conn, *bufio.ReadWriter, error) {
	h, ok := r.ResponseWriter.(http.Hijacker)
	if !ok {
		return nil, nil, fmt.Errorf("response writer does not support hijacking")
	}
	return h.Hijack()
}

func (r *statusRecorder) Flush() {
	if f, ok := r.ResponseWriter.(http.Flusher); ok {
		f.Flush()
	}
}

// LogRequests wraps an HTTP handler and logs each request. Health checks are
// skipped to avoid noise from Docker/orchestrator probes.
func LogRequests(next http.Handler) http.Handler {
	return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		start := time.Now()
		rec := &statusRecorder{ResponseWriter: w, status: http.StatusOK}
		next.ServeHTTP(rec, r)
		if r.URL.Path == "/health" {
			return
		}
		log.Printf("http %s %s -> %d (%s)", r.Method, r.URL.Path, rec.status, time.Since(start).Round(time.Millisecond))
	})
}
