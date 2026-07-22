package db

import (
	"context"
	"encoding/json"
	"errors"

	"github.com/jackc/pgx/v5"
	"github.com/jackc/pgx/v5/pgconn"
)

// ErrActiveCharacterExists is returned when an account already has a living
// character (enforced by a partial unique index).
var ErrActiveCharacterExists = errors.New("account already has an active character")

// Character is a single life belonging to an account.
type Character struct {
	ID        int64
	AccountID int64
	Name      string
	Alive     bool
	X         float64
	Y         float64
	Skills    map[string]int64
	TotalXP   int64
	// Vitals — nil means "full / default" (e.g. brand-new character).
	Hp      *float64
	Stamina *float64
	Mana    *float64
}

// GetActiveCharacter returns the account's living character, or ErrNotFound.
func (d *DB) GetActiveCharacter(ctx context.Context, accountID int64) (Character, error) {
	var c Character
	var skillsJSON []byte
	var totalXp int64
	var hp, stamina, mana *float64
	err := d.Pool.QueryRow(ctx,
		`SELECT id, account_id, name, alive, pos_x, pos_y, skills, total_xp, hp, stamina, mana
		 FROM characters
		 WHERE account_id = $1 AND alive = TRUE
		 LIMIT 1`,
		accountID,
	).Scan(&c.ID, &c.AccountID, &c.Name, &c.Alive, &c.X, &c.Y, &skillsJSON, &totalXp, &hp, &stamina, &mana)
	if errors.Is(err, pgx.ErrNoRows) {
		return Character{}, ErrNotFound
	}
	if err != nil {
		return Character{}, err
	}
	c.Skills = decodeSkills(skillsJSON)
	c.TotalXP = totalXp
	c.Hp = hp
	c.Stamina = stamina
	c.Mana = mana
	return c, nil
}

// CreateCharacter inserts a new living character. Returns
// ErrActiveCharacterExists if one already exists for the account.
func (d *DB) CreateCharacter(ctx context.Context, accountID int64, name string, x, y float64) (Character, error) {
	var c Character
	err := d.Pool.QueryRow(ctx,
		`INSERT INTO characters (account_id, name, pos_x, pos_y)
		 VALUES ($1, $2, $3, $4)
		 RETURNING id, account_id, name, alive, pos_x, pos_y`,
		accountID, name, x, y,
	).Scan(&c.ID, &c.AccountID, &c.Name, &c.Alive, &c.X, &c.Y)
	if err != nil {
		var pgErr *pgconn.PgError
		if errors.As(err, &pgErr) && pgErr.Code == "23505" {
			return Character{}, ErrActiveCharacterExists
		}
		return Character{}, err
	}
	return c, nil
}

// SaveCharacterPosition persists a character's last known position (best-effort,
// used on disconnect so a returning player resumes near where they left).
func (d *DB) SaveCharacterPosition(ctx context.Context, id int64, x, y float64) error {
	_, err := d.Pool.Exec(ctx,
		`UPDATE characters SET pos_x = $2, pos_y = $3 WHERE id = $1 AND alive = TRUE`,
		id, x, y,
	)
	return err
}

// SaveCharacterSkills persists skill XP for a living character.
func (d *DB) SaveCharacterSkills(ctx context.Context, id int64, skills map[string]int64, totalXp int64) error {
	raw, err := json.Marshal(skills)
	if err != nil {
		return err
	}
	_, err = d.Pool.Exec(ctx,
		`UPDATE characters SET skills = $2, total_xp = $3 WHERE id = $1 AND alive = TRUE`,
		id, raw, totalXp,
	)
	return err
}

// SaveCharacterVitals persists current HP / stamina / mana for a living character.
func (d *DB) SaveCharacterVitals(ctx context.Context, id int64, hp, stamina, mana float64) error {
	_, err := d.Pool.Exec(ctx,
		`UPDATE characters SET hp = $2, stamina = $3, mana = $4 WHERE id = $1 AND alive = TRUE`,
		id, hp, stamina, mana,
	)
	return err
}

func decodeSkills(raw []byte) map[string]int64 {
	if len(raw) == 0 {
		return map[string]int64{}
	}
	var m map[string]int64
	if json.Unmarshal(raw, &m) != nil {
		return map[string]int64{}
	}
	return m
}

func (d *DB) MarkCharacterDead(ctx context.Context, id int64) error {
	_, err := d.Pool.Exec(ctx,
		`UPDATE characters SET alive = FALSE, active_buffs = '[]'::jsonb WHERE id = $1 AND alive = TRUE`,
		id,
	)
	return err
}
