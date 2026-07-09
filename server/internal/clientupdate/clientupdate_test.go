package clientupdate

import (
	"net/http"
	"net/http/httptest"
	"strings"
	"testing"
	"time"
)

func TestHandlerDisabled(t *testing.T) {
	t.Setenv("CLIENT_UPDATE_MANIFEST", "")

	h := &Handler{httpClient: &http.Client{Timeout: 15 * time.Second}}
	req := httptest.NewRequest(http.MethodGet, "/client/update", nil)
	rec := httptest.NewRecorder()
	h.ServeHTTP(rec, req)

	if rec.Code != http.StatusNoContent {
		t.Fatalf("status = %d, want 204", rec.Code)
	}
}

func TestNewHandlerUsesPublicRepoByDefault(t *testing.T) {
	t.Setenv("CLIENT_UPDATE_MANIFEST", "")
	t.Setenv("CLIENT_UPDATE_GITHUB_REPO", "")
	t.Setenv("GITHUB_TOKEN", "")

	h := NewHandler()
	if h.githubRepo != defaultGitHubRepo {
		t.Fatalf("githubRepo = %q, want %q", h.githubRepo, defaultGitHubRepo)
	}
}

func TestHandlerStaticManifest(t *testing.T) {
	t.Setenv("CLIENT_UPDATE_MANIFEST", `{
		"enabled": true,
		"latest": "0.2.0",
		"notes": "test",
		"platforms": {
			"win-x64": {"url": "https://example.com/setup.exe", "kind": "installer"}
		}
	}`)
	t.Setenv("CLIENT_UPDATE_GITHUB_REPO", "")

	h := NewHandler()
	req := httptest.NewRequest(http.MethodGet, "/client/update", nil)
	rec := httptest.NewRecorder()
	h.ServeHTTP(rec, req)

	if rec.Code != http.StatusOK {
		t.Fatalf("status = %d, want 200", rec.Code)
	}
	if !strings.Contains(rec.Body.String(), `"latest":"0.2.0"`) {
		t.Fatalf("body = %s", rec.Body.String())
	}
}

func TestHandlerGitHubRelease(t *testing.T) {
	t.Setenv("CLIENT_UPDATE_MANIFEST", "")
	t.Setenv("GITHUB_TOKEN", "")

	srv := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		if r.URL.Path != "/repos/Wolfskii/Deathborn/releases/latest" {
			http.NotFound(w, r)
			return
		}
		w.Header().Set("Content-Type", "application/json")
		_, _ = w.Write([]byte(`{
			"tag_name": "v0.1.23",
			"body": "Installer fixes",
			"assets": [
				{"name": "Deathborn-0.1.23-win-x64-Setup.exe", "browser_download_url": "https://github.com/Wolfskii/Deathborn/releases/download/v0.1.23/Deathborn-0.1.23-win-x64-Setup.exe"},
				{"name": "Deathborn-0.1.23-linux-x64.tar.gz", "browser_download_url": "https://github.com/Wolfskii/Deathborn/releases/download/v0.1.23/Deathborn-0.1.23-linux-x64.tar.gz"},
				{"name": "Deathborn-0.1.23-osx-arm64.dmg", "browser_download_url": "https://github.com/Wolfskii/Deathborn/releases/download/v0.1.23/Deathborn-0.1.23-osx-arm64.dmg"}
			]
		}`))
	}))
	t.Cleanup(srv.Close)

	h := NewHandler()
	h.githubRepo = "Wolfskii/Deathborn"
	h.githubAPIBase = srv.URL
	h.httpClient = srv.Client()

	req := httptest.NewRequest(http.MethodGet, "/client/update", nil)
	rec := httptest.NewRecorder()
	h.ServeHTTP(rec, req)

	if rec.Code != http.StatusOK {
		t.Fatalf("status = %d, want 200; body=%s", rec.Code, rec.Body.String())
	}
	body := rec.Body.String()
	for _, want := range []string{`"latest":"0.1.23"`, `"win-x64"`, `"linux-x64"`, `"osx-arm64"`, `Setup.exe`} {
		if !strings.Contains(body, want) {
			t.Fatalf("body missing %q: %s", want, body)
		}
	}
}

func TestManifestFromGitHubRelease(t *testing.T) {
	m, err := manifestFromGitHubRelease(ghRelease{
		TagName: "v1.2.3",
		Body:    "Notes",
		Assets: []struct {
			Name               string `json:"name"`
			BrowserDownloadURL string `json:"browser_download_url"`
		}{
			{Name: "Deathborn-1.2.3-win-x64-Setup.exe", BrowserDownloadURL: "https://example.com/setup.exe"},
		},
	})
	if err != nil {
		t.Fatal(err)
	}
	if m.Latest != "1.2.3" {
		t.Fatalf("latest = %q, want 1.2.3", m.Latest)
	}
	if m.Platforms["win-x64"].Kind != "installer" {
		t.Fatalf("kind = %q", m.Platforms["win-x64"].Kind)
	}
}
