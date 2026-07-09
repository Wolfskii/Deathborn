package auth

import (
	"encoding/json"
	"errors"
	"log"
	"net/http"
	"strconv"
	"strings"

	"github.com/deathborn/server/internal/db"
	"github.com/deathborn/server/internal/protocol"
)

// Handler serves the register/login HTTP endpoints.
type Handler struct {
	db     *db.DB
	secret string
}

func NewHandler(database *db.DB, secret string) *Handler {
	return &Handler{db: database, secret: secret}
}

type credentials struct {
	Email    string `json:"email"`
	Password string `json:"password"`
}

type tokenResponse struct {
	Token string `json:"token"`
}

// Register creates a new account and returns a JWT.
func (h *Handler) Register(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodPost {
		writeError(w, http.StatusMethodNotAllowed, "method not allowed")
		return
	}
	creds, ok := decodeCredentials(w, r)
	if !ok {
		return
	}
	if !checkClientProtocol(w, r) {
		return
	}

	hash, err := HashPassword(creds.Password)
	if err != nil {
		writeError(w, http.StatusInternalServerError, "could not hash password")
		return
	}

	acc, err := h.db.CreateAccount(r.Context(), creds.Email, hash)
	if errors.Is(err, db.ErrDuplicateEmail) {
		writeError(w, http.StatusConflict, "email already registered")
		return
	}
	if err != nil {
		writeError(w, http.StatusInternalServerError, "could not create account")
		return
	}

	log.Printf("account registered id=%d email=%s", acc.ID, acc.Email)
	h.issue(w, acc.ID)
}

// Login verifies credentials and returns a JWT.
func (h *Handler) Login(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodPost {
		writeError(w, http.StatusMethodNotAllowed, "method not allowed")
		return
	}
	creds, ok := decodeCredentials(w, r)
	if !ok {
		return
	}
	if !checkClientProtocol(w, r) {
		return
	}

	acc, err := h.db.GetAccountByEmail(r.Context(), creds.Email)
	if err != nil {
		writeError(w, http.StatusUnauthorized, "invalid email or password")
		return
	}
	if acc.PasswordHash == nil || !CheckPassword(*acc.PasswordHash, creds.Password) {
		writeError(w, http.StatusUnauthorized, "invalid email or password")
		return
	}

	log.Printf("account login id=%d email=%s", acc.ID, acc.Email)
	h.issue(w, acc.ID)
}

func (h *Handler) issue(w http.ResponseWriter, accountID int64) {
	token, err := IssueToken(h.secret, accountID)
	if err != nil {
		writeError(w, http.StatusInternalServerError, "could not issue token")
		return
	}
	writeJSON(w, http.StatusOK, tokenResponse{Token: token})
}

func decodeCredentials(w http.ResponseWriter, r *http.Request) (credentials, bool) {
	var c credentials
	if err := json.NewDecoder(r.Body).Decode(&c); err != nil {
		writeError(w, http.StatusBadRequest, "invalid request body")
		return credentials{}, false
	}
	c.Email = strings.TrimSpace(strings.ToLower(c.Email))
	if !strings.Contains(c.Email, "@") {
		writeError(w, http.StatusBadRequest, "a valid email is required")
		return credentials{}, false
	}
	if len(c.Password) < 6 {
		writeError(w, http.StatusBadRequest, "password must be at least 6 characters")
		return credentials{}, false
	}
	return c, true
}

func writeJSON(w http.ResponseWriter, status int, v any) {
	w.Header().Set("Content-Type", "application/json")
	w.WriteHeader(status)
	_ = json.NewEncoder(w).Encode(v)
}

func writeError(w http.ResponseWriter, status int, msg string) {
	writeJSON(w, status, map[string]string{"error": msg})
}

func checkClientProtocol(w http.ResponseWriter, r *http.Request) bool {
	clientProto, err := strconv.Atoi(strings.TrimSpace(r.Header.Get(protocol.HeaderName)))
	if err != nil || clientProto <= 0 {
		ok, mismatch := protocol.CheckClient(0)
		_ = ok
		protocol.WriteMismatch(w, mismatch)
		return false
	}
	if ok, mismatch := protocol.CheckClient(clientProto); !ok {
		protocol.WriteMismatch(w, mismatch)
		return false
	}
	return true
}
