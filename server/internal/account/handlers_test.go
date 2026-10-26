package account

import (
	"net/http"
	"net/http/httptest"
	"testing"
)

// These handlers reject bad requests before touching the database, so a nil
// DB is enough for these checks.
func TestHistoryRequiresToken(t *testing.T) {
	h := NewHandler(nil, "secret")
	rec := httptest.NewRecorder()
	h.History(rec, httptest.NewRequest(http.MethodGet, "/history", nil))
	if rec.Code != http.StatusUnauthorized {
		t.Fatalf("status = %d, want 401", rec.Code)
	}
}

func TestMyHighscoresRequiresToken(t *testing.T) {
	h := NewHandler(nil, "secret")
	rec := httptest.NewRecorder()
	h.MyHighscores(rec, httptest.NewRequest(http.MethodGet, "/highscores/me", nil))
	if rec.Code != http.StatusUnauthorized {
		t.Fatalf("status = %d, want 401", rec.Code)
	}
}

func TestHighscoresRejectsUnknownCategory(t *testing.T) {
	h := NewHandler(nil, "secret")
	rec := httptest.NewRecorder()
	h.Highscores(rec, httptest.NewRequest(http.MethodGet, "/highscores?category=gold", nil))
	if rec.Code != http.StatusBadRequest {
		t.Fatalf("status = %d, want 400", rec.Code)
	}
}

func TestEndpointsRejectNonGET(t *testing.T) {
	h := NewHandler(nil, "secret")
	for name, fn := range map[string]http.HandlerFunc{
		"history":       h.History,
		"highscores":    h.Highscores,
		"highscores/me": h.MyHighscores,
		"fallen":        h.Fallen,
	} {
		rec := httptest.NewRecorder()
		fn(rec, httptest.NewRequest(http.MethodPost, "/x", nil))
		if rec.Code != http.StatusMethodNotAllowed {
			t.Fatalf("%s: status = %d, want 405", name, rec.Code)
		}
	}
}

func TestParseLimitClamps(t *testing.T) {
	if got := parseLimit("", 20, 1, 50); got != 20 {
		t.Fatalf("empty = %d, want default 20", got)
	}
	if got := parseLimit("999", 20, 1, 50); got != 50 {
		t.Fatalf("too big = %d, want 50", got)
	}
	if got := parseLimit("0", 20, 1, 50); got != 1 {
		t.Fatalf("zero = %d, want 1", got)
	}
}

func TestFallenRejectsNonGET(t *testing.T) {
	h := NewHandler(nil, "secret")
	rec := httptest.NewRecorder()
	h.Fallen(rec, httptest.NewRequest(http.MethodPost, "/fallen", nil))
	if rec.Code != http.StatusMethodNotAllowed {
		t.Fatalf("status = %d, want 405", rec.Code)
	}
}
