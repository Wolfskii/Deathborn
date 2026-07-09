package clientupdate

import (
	"net/http"
	"net/http/httptest"
	"strings"
	"testing"
)

func TestHandlerDisabled(t *testing.T) {
	t.Setenv("CLIENT_UPDATE_MANIFEST", "")

	h := NewHandler()
	req := httptest.NewRequest(http.MethodGet, "/client/update", nil)
	rec := httptest.NewRecorder()
	h.ServeHTTP(rec, req)

	if rec.Code != http.StatusNoContent {
		t.Fatalf("status = %d, want 204", rec.Code)
	}
}

func TestHandlerActive(t *testing.T) {
	t.Setenv("CLIENT_UPDATE_MANIFEST", `{
		"enabled": true,
		"latest": "0.2.0",
		"notes": "test",
		"platforms": {
			"win-x64": {"url": "https://example.com/setup.exe", "kind": "installer"}
		}
	}`)

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
