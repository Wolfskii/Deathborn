// Package account serves the read-only history and highscore endpoints.
package account

import (
	"encoding/json"
	"log"
	"net/http"
	"strconv"
	"time"

	"github.com/deathborn/server/internal/auth"
	"github.com/deathborn/server/internal/db"
)

// Handler serves /history, /highscores and /highscores/me.
type Handler struct {
	db     *db.DB
	secret string
}

func NewHandler(database *db.DB, secret string) *Handler {
	return &Handler{db: database, secret: secret}
}

type lifeJSON struct {
	Name            string           `json:"name"`
	Reason          string           `json:"reason"`
	KillerName      string           `json:"killerName"`
	Epitaph         string           `json:"epitaph"`
	TotalXP         int64            `json:"totalXp"`
	SurvivalSeconds int64            `json:"survivalSeconds"`
	PvPKills        int              `json:"pvpKills"`
	Wealth          int64            `json:"wealth"`
	Skills          map[string]int64 `json:"skills"`
	DiedAt          time.Time        `json:"diedAt"`
}

// History returns the caller's past lives, newest first. Requires a token.
func (h *Handler) History(w http.ResponseWriter, r *http.Request) {
	if !requireGET(w, r) {
		return
	}
	accountID, err := auth.AccountFromRequest(h.secret, r)
	if err != nil {
		writeError(w, http.StatusUnauthorized, "unauthorized")
		return
	}
	limit := parseLimit(r.URL.Query().Get("limit"), 20, 1, 50)

	rows, err := h.db.ListCharacterHistory(r.Context(), accountID, limit)
	if err != nil {
		log.Printf("history account_id=%d: %v", accountID, err)
		writeError(w, http.StatusInternalServerError, "could not load history")
		return
	}
	lives := make([]lifeJSON, 0, len(rows))
	for _, row := range rows {
		lives = append(lives, lifeJSON{
			Name: row.Name, Reason: row.Reason, KillerName: row.KillerName, Epitaph: row.Epitaph,
			TotalXP: row.TotalXP, SurvivalSeconds: row.SurvivalSeconds, PvPKills: row.PvPKills,
			Wealth: row.Wealth, Skills: row.Skills, DiedAt: row.DiedAt,
		})
	}
	writeJSON(w, http.StatusOK, map[string]any{"lives": lives})
}

type scoreEntryJSON struct {
	Rank  int    `json:"rank"`
	Name  string `json:"name"`
	Value int64  `json:"value"`
}

// Highscores returns the public leaderboard for one category.
func (h *Handler) Highscores(w http.ResponseWriter, r *http.Request) {
	if !requireGET(w, r) {
		return
	}
	category := r.URL.Query().Get("category")
	if _, err := db.ScoreColumn(category); err != nil {
		writeError(w, http.StatusBadRequest, "unknown category")
		return
	}
	limit := parseLimit(r.URL.Query().Get("limit"), 20, 1, 100)

	rows, err := h.db.TopHighscores(r.Context(), category, limit)
	if err != nil {
		log.Printf("highscores category=%s: %v", category, err)
		writeError(w, http.StatusInternalServerError, "could not load highscores")
		return
	}
	entries := make([]scoreEntryJSON, 0, len(rows))
	for i, row := range rows {
		entries = append(entries, scoreEntryJSON{Rank: i + 1, Name: row.Name, Value: row.Value})
	}
	writeJSON(w, http.StatusOK, map[string]any{"category": category, "entries": entries})
}

// MyHighscores returns the caller's best values and ranks. Requires a token.
func (h *Handler) MyHighscores(w http.ResponseWriter, r *http.Request) {
	if !requireGET(w, r) {
		return
	}
	accountID, err := auth.AccountFromRequest(h.secret, r)
	if err != nil {
		writeError(w, http.StatusUnauthorized, "unauthorized")
		return
	}
	scores, err := h.db.MyHighscores(r.Context(), accountID)
	if err != nil {
		log.Printf("my highscores account_id=%d: %v", accountID, err)
		writeError(w, http.StatusInternalServerError, "could not load highscores")
		return
	}
	writeJSON(w, http.StatusOK, map[string]any{"values": scores.Values, "ranks": scores.Ranks})
}

func requireGET(w http.ResponseWriter, r *http.Request) bool {
	if r.Method != http.MethodGet {
		writeError(w, http.StatusMethodNotAllowed, "method not allowed")
		return false
	}
	return true
}

// parseLimit reads an integer query value, falling back to def and clamping to [lo, hi].
func parseLimit(raw string, def, lo, hi int) int {
	n, err := strconv.Atoi(raw)
	if err != nil {
		return def
	}
	if n < lo {
		return lo
	}
	if n > hi {
		return hi
	}
	return n
}

func writeJSON(w http.ResponseWriter, status int, v any) {
	w.Header().Set("Content-Type", "application/json")
	w.WriteHeader(status)
	_ = json.NewEncoder(w).Encode(v)
}

func writeError(w http.ResponseWriter, status int, msg string) {
	writeJSON(w, status, map[string]string{"error": msg})
}

type fallenJSON struct {
	Name            string    `json:"name"`
	Reason          string    `json:"reason"`
	KillerName      string    `json:"killerName"`
	Epitaph         string    `json:"epitaph"`
	SurvivalSeconds int64     `json:"survivalSeconds"`
	DiedAt          time.Time `json:"diedAt"`
}

// Fallen returns the most recent deaths on the server, newest first. Public:
// it shows names, causes and last words, never account ids.
func (h *Handler) Fallen(w http.ResponseWriter, r *http.Request) {
	if !requireGET(w, r) {
		return
	}
	limit := parseLimit(r.URL.Query().Get("limit"), 20, 1, 50)

	rows, err := h.db.ListRecentDeaths(r.Context(), limit)
	if err != nil {
		log.Printf("fallen: %v", err)
		writeError(w, http.StatusInternalServerError, "could not load the hall of the fallen")
		return
	}
	deaths := make([]fallenJSON, 0, len(rows))
	for _, row := range rows {
		deaths = append(deaths, fallenJSON{
			Name: row.Name, Reason: row.Reason, KillerName: row.KillerName, Epitaph: row.Epitaph,
			SurvivalSeconds: row.SurvivalSeconds, DiedAt: row.DiedAt,
		})
	}
	writeJSON(w, http.StatusOK, map[string]any{"deaths": deaths})
}
