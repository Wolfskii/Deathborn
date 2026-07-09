package clientupdate

import (
	"encoding/json"
	"fmt"
	"net/http"
	"regexp"
	"strings"
	"time"
)

const githubCacheTTL = 5 * time.Minute

var platformAssetPatterns = []struct {
	Runtime string
	Kind    string
	Pattern *regexp.Regexp
}{
	{"win-x64", "installer", regexp.MustCompile(`^Deathborn-.*-win-x64-Setup\.exe$`)},
	{"linux-x64", "archive", regexp.MustCompile(`^Deathborn-.*-linux-x64\.tar\.gz$`)},
	{"osx-x64", "dmg", regexp.MustCompile(`^Deathborn-.*-osx-x64\.dmg$`)},
	{"osx-arm64", "dmg", regexp.MustCompile(`^Deathborn-.*-osx-arm64\.dmg$`)},
}

type ghRelease struct {
	TagName string `json:"tag_name"`
	Body    string `json:"body"`
	Assets  []struct {
		Name               string `json:"name"`
		BrowserDownloadURL string `json:"browser_download_url"`
	} `json:"assets"`
}

func manifestFromGitHubRelease(rel ghRelease) (Manifest, error) {
	tag := strings.TrimSpace(rel.TagName)
	if tag == "" {
		return Manifest{}, fmt.Errorf("release has no tag")
	}

	latest := strings.TrimPrefix(strings.TrimPrefix(tag, "v"), "V")
	platforms := make(map[string]PlatformArtifact)

	for _, spec := range platformAssetPatterns {
		for _, asset := range rel.Assets {
			if !spec.Pattern.MatchString(asset.Name) {
				continue
			}
			if strings.TrimSpace(asset.BrowserDownloadURL) == "" {
				continue
			}
			platforms[spec.Runtime] = PlatformArtifact{
				URL:  asset.BrowserDownloadURL,
				Kind: spec.Kind,
			}
			break
		}
	}

	if len(platforms) == 0 {
		return Manifest{}, fmt.Errorf("release %s has no client installer assets", tag)
	}

	notes := strings.TrimSpace(rel.Body)
	if len(notes) > 400 {
		notes = notes[:397] + "..."
	}

	return Manifest{
		Enabled:   true,
		Latest:    latest,
		Notes:     notes,
		Platforms: platforms,
	}, nil
}

func (h *Handler) githubManifest(now time.Time) (Manifest, bool, error) {
	if h.githubRepo == "" {
		return Manifest{}, false, nil
	}

	h.mu.Lock()
	if h.cacheActive && now.Sub(h.cacheAt) < githubCacheTTL {
		m := h.cached
		h.mu.Unlock()
		return m, true, nil
	}
	h.mu.Unlock()

	m, err := h.fetchGitHubManifest()
	if err != nil {
		return Manifest{}, false, err
	}

	h.mu.Lock()
	h.cached = m
	h.cacheActive = true
	h.cacheAt = now
	h.mu.Unlock()

	return m, true, nil
}

func (h *Handler) fetchGitHubManifest() (Manifest, error) {
	base := strings.TrimRight(h.githubAPIBase, "/")
	if base == "" {
		base = "https://api.github.com"
	}
	url := fmt.Sprintf("%s/repos/%s/releases/latest", base, h.githubRepo)
	req, err := http.NewRequest(http.MethodGet, url, nil)
	if err != nil {
		return Manifest{}, err
	}
	req.Header.Set("Accept", "application/vnd.github+json")
	req.Header.Set("User-Agent", "deathborn-server")
	if h.githubToken != "" {
		req.Header.Set("Authorization", "Bearer "+h.githubToken)
	}

	resp, err := h.httpClient.Do(req)
	if err != nil {
		return Manifest{}, err
	}
	defer resp.Body.Close()

	if resp.StatusCode == http.StatusNotFound {
		return Manifest{}, fmt.Errorf("no published GitHub release for %s", h.githubRepo)
	}
	if resp.StatusCode != http.StatusOK {
		return Manifest{}, fmt.Errorf("github api %s: %s", url, resp.Status)
	}

	var rel ghRelease
	if err := json.NewDecoder(resp.Body).Decode(&rel); err != nil {
		return Manifest{}, err
	}

	return manifestFromGitHubRelease(rel)
}
