// Package clientupdate serves optional client auto-update metadata for GET /client/update.
// Configure with CLIENT_UPDATE_MANIFEST (JSON). Works with private repos — host installers on
// your API/CDN and point URLs in the manifest (not GitHub Releases).
package clientupdate

import (
	"encoding/json"
	"net/http"
	"os"
	"strings"
)

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
	manifest Manifest
	active   bool
}

// NewHandler loads CLIENT_UPDATE_MANIFEST JSON from the environment.
func NewHandler() *Handler {
	raw := strings.TrimSpace(os.Getenv("CLIENT_UPDATE_MANIFEST"))
	if raw == "" {
		return &Handler{}
	}
	var m Manifest
	if err := json.Unmarshal([]byte(raw), &m); err != nil {
		return &Handler{}
	}
	if !m.Enabled || strings.TrimSpace(m.Latest) == "" || len(m.Platforms) == 0 {
		return &Handler{manifest: m}
	}
	return &Handler{manifest: m, active: true}
}

func (h *Handler) ServeHTTP(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodGet {
		http.Error(w, "method not allowed", http.StatusMethodNotAllowed)
		return
	}
	if !h.active {
		w.WriteHeader(http.StatusNoContent)
		return
	}
	w.Header().Set("Content-Type", "application/json")
	_ = json.NewEncoder(w).Encode(h.manifest)
}
