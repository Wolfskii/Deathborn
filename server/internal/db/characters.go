package db

import (
	"context"
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
}

// GetActiveCharacter returns the account's living character, or ErrNotFound.
func (d *DB) GetActiveCharacter(ctx context.Context, accountID int64) (Character, error) {
	var c Character
	err := d.Pool.QueryRow(ctx,
		`SELECT id, account_id, name, alive, pos_x, pos_y
		 FROM characters
		 WHERE account_id = $1 AND alive = TRUE
		 LIMIT 1`,
		accountID,
	).Scan(&c.ID, &c.AccountID, &c.Name, &c.Alive, &c.X, &c.Y)
	if errors.Is(err, pgx.ErrNoRows) {
		return Character{}, ErrNotFound
	}
	if err != nil {
		return Character{}, err
	}
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

// MarkCharacterDead sets alive=false for a character that has died.
func (d *DB) MarkCharacterDead(ctx context.Context, id int64) error {
	_, err := d.Pool.Exec(ctx,
		`UPDATE characters SET alive = FALSE WHERE id = $1 AND alive = TRUE`,
		id,
	)
	return err
}
