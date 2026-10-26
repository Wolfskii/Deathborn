package net

import (
	"strings"
	"unicode"
	"unicode/utf8"
)

const maxChatRunes = 120

// sanitizeChat trims the text, drops control characters, and rejects empty or
// over-long messages. The limit counts characters, not bytes.
func sanitizeChat(s string) (string, bool) {
	s = strings.Map(func(r rune) rune {
		if unicode.IsControl(r) {
			return -1
		}
		return r
	}, s)
	s = strings.TrimSpace(s)
	if s == "" || utf8.RuneCountInString(s) > maxChatRunes {
		return "", false
	}
	return s, true
}
