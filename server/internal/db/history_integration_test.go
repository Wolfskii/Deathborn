package db

import (
	"context"
	"encoding/json"
	"fmt"
	"os"
	"testing"
	"time"

	"github.com/deathborn/server/migrations"
)

// TestDeathRecordsIntegration runs against a real PostgreSQL database. It is
// skipped unless DEATHBORN_TEST_DATABASE_URL is set, for example:
//
//	DEATHBORN_TEST_DATABASE_URL=postgres://user:pass@localhost:5432/db go test ./internal/db/
func TestDeathRecordsIntegration(t *testing.T) {
	url := os.Getenv("DEATHBORN_TEST_DATABASE_URL")
	if url == "" {
		t.Skip("set DEATHBORN_TEST_DATABASE_URL to run database integration tests")
	}
	ctx := context.Background()
	pool, err := Connect(ctx, url)
	if err != nil {
		t.Fatalf("connect: %v", err)
	}
	defer pool.Close()
	if err := RunMigrations(ctx, pool, migrations.Files); err != nil {
		t.Fatalf("migrate: %v", err)
	}
	database := New(pool)

	stamp := time.Now().UnixNano()
	acc, err := database.CreateAccount(ctx, fmt.Sprintf("it-%d@test.invalid", stamp), "hash")
	if err != nil {
		t.Fatalf("create account: %v", err)
	}
	t.Cleanup(func() {
		_, _ = pool.Exec(context.Background(), `DELETE FROM accounts WHERE id = $1`, acc.ID)
	})

	char, err := database.CreateCharacter(ctx, acc.ID, fmt.Sprintf("Tester%d", stamp%1e9), "human",
		json.RawMessage(`{}`), 0, 0)
	if err != nil {
		t.Fatalf("create character: %v", err)
	}

	if err := database.AddPvPKill(ctx, char.ID); err != nil {
		t.Fatalf("add pvp kill: %v", err)
	}

	skills := map[string]int64{"attack": 500, "mining": 2000}
	rec := DeathRecord{
		CharacterID: char.ID,
		TotalXP:     2500,
		Skills:      skills,
		SkillLevel:  12,
		Wealth:      7,
		Reason:      "Slain by Rowan",
		KillerName:  "Rowan",
		Epitaph:     "not today",
	}
	out, err := database.RecordDeath(ctx, rec)
	if err != nil {
		t.Fatalf("record death: %v", err)
	}
	if out.PvPKills != 1 {
		t.Fatalf("pvp kills = %d, want 1", out.PvPKills)
	}

	// Recording the same death again must do nothing.
	again, err := database.RecordDeath(ctx, rec)
	if err != nil {
		t.Fatalf("second record death: %v", err)
	}
	if again != (DeathOutcome{}) {
		t.Fatalf("second record returned %+v, want zero outcome", again)
	}

	var count int
	if err := pool.QueryRow(ctx,
		`SELECT count(*) FROM character_history WHERE account_id = $1`, acc.ID).Scan(&count); err != nil {
		t.Fatalf("count history: %v", err)
	}
	if count != 1 {
		t.Fatalf("history rows = %d, want 1 (death must be recorded once)", count)
	}

	lives, err := database.ListCharacterHistory(ctx, acc.ID, 10)
	if err != nil {
		t.Fatalf("list history: %v", err)
	}
	if len(lives) != 1 || lives[0].Reason != "Slain by Rowan" || lives[0].Epitaph != "not today" {
		t.Fatalf("unexpected history: %+v", lives)
	}
	if lives[0].Skills["mining"] != 2000 {
		t.Fatalf("skills not round-tripped: %+v", lives[0].Skills)
	}

	recent, err := database.ListRecentDeaths(ctx, 50)
	if err != nil {
		t.Fatalf("list recent deaths: %v", err)
	}
	var found bool
	for _, d := range recent {
		if d.Name == char.Name && d.Epitaph == "not today" {
			found = true
		}
	}
	if !found {
		t.Fatal("the death should appear in the hall of the fallen")
	}

	mine, err := database.MyHighscores(ctx, acc.ID)
	if err != nil {
		t.Fatalf("my highscores: %v", err)
	}
	if mine.Values["xp"] != 2500 || mine.Values["level"] != 12 || mine.Values["pvp"] != 1 {
		t.Fatalf("unexpected highscore values: %+v", mine.Values)
	}
	if mine.Ranks["xp"] < 1 {
		t.Fatalf("xp rank = %d, want >= 1", mine.Ranks["xp"])
	}

	top, err := database.TopHighscores(ctx, "xp", 100)
	if err != nil {
		t.Fatalf("top highscores: %v", err)
	}
	if len(top) == 0 {
		t.Fatal("top highscores is empty")
	}

	if _, err := database.TopHighscores(ctx, "gold; DROP TABLE accounts", 10); err == nil {
		t.Fatal("unknown category must be rejected")
	}
}
