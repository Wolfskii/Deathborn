package net

import (
	"testing"

	"github.com/deathborn/server/internal/game"
)

func TestValidateCharacterAppearanceUsesDefaults(t *testing.T) {
	race, appearance, ok := validateCharacterAppearance("", game.PlayerAppearance{})
	if !ok || race != "human" {
		t.Fatalf("default appearance rejected: race=%q ok=%v", race, ok)
	}
	if appearance.SkinTone != "fair" || appearance.HairStyleID != "farm-hair-josh-brown" {
		t.Fatalf("unexpected defaults: %#v", appearance)
	}
}

func TestValidateCharacterAppearanceRejectsInvalidValues(t *testing.T) {
	_, _, ok := validateCharacterAppearance("dragon", defaultPlayerAppearance())
	if ok {
		t.Fatal("unknown race was accepted")
	}

	appearance := defaultPlayerAppearance()
	appearance.HairStyleID = "arbitrary-layer"
	_, _, ok = validateCharacterAppearance("elf", appearance)
	if ok {
		t.Fatal("unknown layer was accepted")
	}

	appearance = defaultPlayerAppearance()
	appearance.HairBrightness = 101
	_, _, ok = validateCharacterAppearance("elf", appearance)
	if ok {
		t.Fatal("out-of-range palette value was accepted")
	}
}
