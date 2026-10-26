package net

import (
	"strings"
	"testing"
)

func TestSanitizeChat(t *testing.T) {
	tests := []struct {
		name   string
		in     string
		want   string
		wantOK bool
	}{
		{"empty", "", "", false},
		{"whitespace only", "   \t ", "", false},
		{"trims spaces", "  hello  ", "hello", true},
		{"120 runes ok", strings.Repeat("a", 120), strings.Repeat("a", 120), true},
		{"121 runes rejected", strings.Repeat("a", 121), "", false},
		{"null byte removed", "he\x00llo", "hello", true},
		{"newline removed", "hi\nthere", "hithere", true},
		{"multibyte counted as runes", strings.Repeat("é", 120), strings.Repeat("é", 120), true},
		{"multibyte over limit", strings.Repeat("é", 121), "", false},
	}
	for _, tc := range tests {
		t.Run(tc.name, func(t *testing.T) {
			got, ok := sanitizeChat(tc.in)
			if ok != tc.wantOK || got != tc.want {
				t.Fatalf("sanitizeChat(%q) = (%q, %v), want (%q, %v)", tc.in, got, ok, tc.want, tc.wantOK)
			}
		})
	}
}
