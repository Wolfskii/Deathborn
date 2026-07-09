// Package clientupdate serves client auto-update metadata for GET /client/update.
// By default it reads the latest published release from the public GitHub repo (see defaultGitHubRepo).
// Optional env overrides: CLIENT_UPDATE_MANIFEST (static JSON), CLIENT_UPDATE_GITHUB_REPO, GITHUB_TOKEN.
package clientupdate

import (
	"encoding/json"
	"net/http"
	"os"
	"strings"
	"sync"
	"time"
)

const defaultGitHubRepo = "Wolfskii/Deathborn"

// Manifest describes the newest client build per platform.
type Manifest struct {
	Enabled   bool                        `json:"enabled"`
	Latest    string                      `json:"latest"`
	Notes     string                      `json:"notes,omitempty"`
	Platforms map[string]PlatformArtifact `json:"platforms"`
}

// PlatformArtifact is a downloadable build for one runtime id (win-x64, linux-x64, …).
type PlatformArtifact struct {
	URL    string `json:"url"`
	Kind   string `json:"kind"` // installer, archive, dmg
	SHA256 string `json:"sha256,omitempty"`
}

// Handler returns update metadata or 204 when updates are disabled/unconfigured.
type Handler struct {
	staticManifest Manifest
	staticActive   bool

	githubRepo    string
	githubToken   string
	githubAPIBase string
	httpClient    *http.Client

	mu          sync.Mutex
	cached      Manifest
	cacheActive bool
	cacheAt     time.Time
}

// NewHandler loads update settings from the environment.
func NewHandler() *Handler {
	h := &Handler{
		httpClient: &http.Client{Timeout: 15 * time.Second},
	}

	if raw := strings.TrimSpace(os.Getenv("CLIENT_UPDATE_MANIFEST")); raw != "" {
		var m Manifest
		if err := json.Unmarshal([]byte(raw), &m); err == nil {
			h.staticManifest = m
			if m.Enabled && strings.TrimSpace(m.Latest) != "" && len(m.Platforms) > 0 {
				h.staticActive = true
			}
		}
	}

	h.githubRepo = defaultGitHubRepo
	if v := strings.TrimSpace(os.Getenv("CLIENT_UPDATE_GITHUB_REPO")); v != "" {
		h.githubRepo = v
	}
	h.githubToken = strings.TrimSpace(os.Getenv("GITHUB_TOKEN"))

	return h
}

func (h *Handler) ServeHTTP(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodGet {
		http.Error(w, "method not allowed", http.StatusMethodNotAllowed)
		return
	}

	if h.staticActive {
		writeManifest(w, h.staticManifest)
		return
	}

	manifest, ok, err := h.githubManifest(time.Now())
	if err != nil || !ok {
		w.WriteHeader(http.StatusNoContent)
		return
	}

	writeManifest(w, manifest)
}

func writeManifest(w http.ResponseWriter, manifest Manifest) {
	w.Header().Set("Content-Type", "application/json")
	_ = json.NewEncoder(w).Encode(manifest)
}
