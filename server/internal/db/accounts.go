package db

import (
	"context"
	"errors"
	"time"

	"github.com/jackc/pgx/v5"
	"github.com/jackc/pgx/v5/pgconn"
)

// ErrDuplicateEmail is returned when an account already exists for an email.
var ErrDuplicateEmail = errors.New("email already registered")

// Account is the persistent identity. PasswordHash is nil for OAuth-only
// accounts (not used in M0).
type Account struct {
	ID           int64
	Email        string
	PasswordHash *string
	CreatedAt    time.Time
}

// CreateAccount inserts a new email/password account.
func (d *DB) CreateAccount(ctx context.Context, email, passwordHash string) (Account, error) {
	var a Account
	err := d.Pool.QueryRow(ctx,
		`INSERT INTO accounts (email, password_hash)
		 VALUES ($1, $2)
		 RETURNING id, email, password_hash, created_at`,
		email, passwordHash,
	).Scan(&a.ID, &a.Email, &a.PasswordHash, &a.CreatedAt)
	if err != nil {
		var pgErr *pgconn.PgError
		if errors.As(err, &pgErr) && pgErr.Code == "23505" {
			return Account{}, ErrDuplicateEmail
		}
		return Account{}, err
	}
	return a, nil
}

// GetAccountByEmail looks up an account; returns ErrNotFound if absent.
func (d *DB) GetAccountByEmail(ctx context.Context, email string) (Account, error) {
	var a Account
	err := d.Pool.QueryRow(ctx,
		`SELECT id, email, password_hash, created_at FROM accounts WHERE email = $1`,
		email,
	).Scan(&a.ID, &a.Email, &a.PasswordHash, &a.CreatedAt)
	if errors.Is(err, pgx.ErrNoRows) {
		return Account{}, ErrNotFound
	}
	if err != nil {
		return Account{}, err
	}
	return a, nil
}
