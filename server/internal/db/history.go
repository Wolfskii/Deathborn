package db

import (
	"context"
	"encoding/json"
	"errors"
	"fmt"
	"time"

	"github.com/jackc/pgx/v5"
)

// DeathRecord is everything written to character_history and highscores when a
// character dies. The caller reads it from the in-memory world before removal.
type DeathRecord struct {
	CharacterID int64
	TotalXP     int64
	Skills      map[string]int64
	SkillLevel  int   // highest single skill level
	Wealth      int64 // item count for now; gold later
	Reason      string
	KillerName  string
	Epitaph     string // last chat line, may be ""
}

// DeathOutcome holds values computed by the database during RecordDeath.
type DeathOutcome struct {
	SurvivalSeconds int64
	PvPKills        int
}

// RecordDeath writes character_history, upserts highscores and marks the
// character dead in one transaction. If the character is already dead it does
// nothing and returns a zero DeathOutcome with no error.
func (d *DB) RecordDeath(ctx context.Context, r DeathRecord) (DeathOutcome, error) {
	var out DeathOutcome
	skillsJSON, err := json.Marshal(r.Skills)
	if err != nil {
		return out, err
	}

	tx, err := d.Pool.Begin(ctx)
	if err != nil {
		return out, err
	}
	defer tx.Rollback(ctx)

	var accountID int64
	err = tx.QueryRow(ctx,
		`INSERT INTO character_history
		   (account_id, name, total_xp, skills, survival_seconds, pvp_kills, wealth, death_reason, killer_name, epitaph)
		 SELECT account_id, name, $2::bigint, $3::jsonb,
		        EXTRACT(EPOCH FROM (now() - created_at))::bigint, pvp_kills, $4::bigint, $5::text, $6::text, $7::text
		 FROM characters WHERE id = $1 AND alive = TRUE
		 RETURNING account_id, survival_seconds, pvp_kills`,
		r.CharacterID, r.TotalXP, skillsJSON, r.Wealth, r.Reason, r.KillerName, r.Epitaph,
	).Scan(&accountID, &out.SurvivalSeconds, &out.PvPKills)
	if errors.Is(err, pgx.ErrNoRows) {
		return DeathOutcome{}, nil // already dead
	}
	if err != nil {
		return DeathOutcome{}, err
	}

	_, err = tx.Exec(ctx,
		`INSERT INTO highscores
		   (account_id, longest_survival_seconds, highest_total_xp, most_pvp_kills, highest_wealth, highest_skill_level, updated_at)
		 VALUES ($1, $2, $3, $4, $5, $6, now())
		 ON CONFLICT (account_id) DO UPDATE SET
		   longest_survival_seconds = GREATEST(highscores.longest_survival_seconds, EXCLUDED.longest_survival_seconds),
		   highest_total_xp = GREATEST(highscores.highest_total_xp, EXCLUDED.highest_total_xp),
		   most_pvp_kills = GREATEST(highscores.most_pvp_kills, EXCLUDED.most_pvp_kills),
		   highest_wealth = GREATEST(highscores.highest_wealth, EXCLUDED.highest_wealth),
		   highest_skill_level = GREATEST(highscores.highest_skill_level, EXCLUDED.highest_skill_level),
		   updated_at = now()`,
		accountID, out.SurvivalSeconds, r.TotalXP, out.PvPKills, r.Wealth, r.SkillLevel,
	)
	if err != nil {
		return DeathOutcome{}, err
	}

	if _, err := tx.Exec(ctx,
		`UPDATE characters SET alive = FALSE, active_buffs = '[]'::jsonb WHERE id = $1 AND alive = TRUE`,
		r.CharacterID); err != nil {
		return DeathOutcome{}, err
	}

	return out, tx.Commit(ctx)
}

// AddPvPKill increments a living character's kill counter.
func (d *DB) AddPvPKill(ctx context.Context, characterID int64) error {
	_, err := d.Pool.Exec(ctx,
		`UPDATE characters SET pvp_kills = pvp_kills + 1 WHERE id = $1 AND alive = TRUE`, characterID)
	return err
}

// HistoryRow is one past life, as shown on the Past Lives screen.
type HistoryRow struct {
	Name, Reason, KillerName, Epitaph string
	TotalXP, SurvivalSeconds, Wealth  int64
	PvPKills                          int
	Skills                            map[string]int64
	DiedAt                            time.Time
}

const historyColumns = `name, death_reason, killer_name, epitaph, total_xp, survival_seconds,
	pvp_kills, wealth, skills, died_at`

func scanHistory(rows pgx.Rows) ([]HistoryRow, error) {
	defer rows.Close()
	out := []HistoryRow{}
	for rows.Next() {
		var h HistoryRow
		var skillsRaw []byte
		if err := rows.Scan(&h.Name, &h.Reason, &h.KillerName, &h.Epitaph, &h.TotalXP,
			&h.SurvivalSeconds, &h.PvPKills, &h.Wealth, &skillsRaw, &h.DiedAt); err != nil {
			return nil, err
		}
		h.Skills = decodeSkills(skillsRaw)
		out = append(out, h)
	}
	return out, rows.Err()
}

// ListCharacterHistory returns one account's past lives, newest first.
func (d *DB) ListCharacterHistory(ctx context.Context, accountID int64, limit int) ([]HistoryRow, error) {
	rows, err := d.Pool.Query(ctx,
		`SELECT `+historyColumns+` FROM character_history
		 WHERE account_id = $1 ORDER BY died_at DESC LIMIT $2`, accountID, limit)
	if err != nil {
		return nil, err
	}
	return scanHistory(rows)
}

// ListRecentDeaths returns the newest deaths across all accounts.
func (d *DB) ListRecentDeaths(ctx context.Context, limit int) ([]HistoryRow, error) {
	rows, err := d.Pool.Query(ctx,
		`SELECT `+historyColumns+` FROM character_history ORDER BY died_at DESC LIMIT $1`, limit)
	if err != nil {
		return nil, err
	}
	return scanHistory(rows)
}

// scoreColumns maps public category names to highscores columns. Only these
// columns are ever placed into SQL text; user input is never interpolated.
var scoreColumns = map[string]string{
	"survival": "longest_survival_seconds",
	"xp":       "highest_total_xp",
	"pvp":      "most_pvp_kills",
	"wealth":   "highest_wealth",
	"level":    "highest_skill_level",
}

// ScoreColumn returns the highscores column for a category, or ErrUnknownCategory.
func ScoreColumn(category string) (string, error) {
	col, ok := scoreColumns[category]
	if !ok {
		return "", ErrUnknownCategory
	}
	return col, nil
}

// ErrUnknownCategory is returned for a leaderboard category that does not exist.
var ErrUnknownCategory = errors.New("unknown highscore category")

// ScoreRow is one leaderboard entry.
type ScoreRow struct {
	Name  string
	Value int64
}

// TopHighscores returns the best values for a category. Accounts with a zero
// value are left out.
func (d *DB) TopHighscores(ctx context.Context, category string, limit int) ([]ScoreRow, error) {
	col, err := ScoreColumn(category)
	if err != nil {
		return nil, err
	}
	rows, err := d.Pool.Query(ctx, fmt.Sprintf(
		`SELECT COALESCE((SELECT c.name FROM characters c WHERE c.account_id = h.account_id
		                  ORDER BY c.created_at DESC LIMIT 1), 'Unknown'),
		        h.%[1]s
		 FROM highscores h WHERE h.%[1]s > 0
		 ORDER BY h.%[1]s DESC LIMIT $1`, col), limit)
	if err != nil {
		return nil, err
	}
	defer rows.Close()
	out := []ScoreRow{}
	for rows.Next() {
		var s ScoreRow
		if err := rows.Scan(&s.Name, &s.Value); err != nil {
			return nil, err
		}
		out = append(out, s)
	}
	return out, rows.Err()
}

// MyScores holds one account's best value and rank per category. A rank of 0
// means the account has no score in that category yet.
type MyScores struct {
	Values map[string]int64
	Ranks  map[string]int64
}

// MyHighscores returns an account's best values and their ranks.
func (d *DB) MyHighscores(ctx context.Context, accountID int64) (MyScores, error) {
	out := MyScores{Values: map[string]int64{}, Ranks: map[string]int64{}}
	for category, col := range scoreColumns {
		var value int64
		err := d.Pool.QueryRow(ctx, fmt.Sprintf(
			`SELECT COALESCE((SELECT %[1]s FROM highscores WHERE account_id = $1), 0)`, col),
			accountID).Scan(&value)
		if err != nil {
			return out, err
		}
		out.Values[category] = value
		if value == 0 {
			continue
		}
		var above int64
		if err := d.Pool.QueryRow(ctx, fmt.Sprintf(
			`SELECT count(*) FROM highscores WHERE %[1]s > $1`, col), value).Scan(&above); err != nil {
			return out, err
		}
		out.Ranks[category] = above + 1
	}
	return out, nil
}
