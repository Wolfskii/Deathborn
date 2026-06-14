// Package db owns the PostgreSQL connection pool, a tiny idempotent migration
// runner, and all SQL queries for the server.
package db

import (
	"context"
	"errors"
	"fmt"
	"io/fs"
	"sort"

	"github.com/jackc/pgx/v5/pgxpool"
)

// ErrNotFound is returned by queries when no matching row exists.
var ErrNotFound = errors.New("not found")

// DB wraps the connection pool and exposes typed query methods.
type DB struct {
	Pool *pgxpool.Pool
}

// Connect opens a pgx connection pool and verifies connectivity.
func Connect(ctx context.Context, url string) (*pgxpool.Pool, error) {
	pool, err := pgxpool.New(ctx, url)
	if err != nil {
		return nil, err
	}
	if err := pool.Ping(ctx); err != nil {
		pool.Close()
		return nil, err
	}
	return pool, nil
}

// New wraps an existing pool.
func New(pool *pgxpool.Pool) *DB { return &DB{Pool: pool} }

// RunMigrations applies any *.sql files in fsys that have not yet been applied,
// tracked by a schema_migrations table. Each file runs in its own transaction.
// Schema files are written to be idempotent (IF NOT EXISTS), so re-running is
// safe even if the tracking table is lost.
func RunMigrations(ctx context.Context, pool *pgxpool.Pool, fsys fs.FS) error {
	_, err := pool.Exec(ctx, `CREATE TABLE IF NOT EXISTS schema_migrations (
		version    TEXT PRIMARY KEY,
		applied_at TIMESTAMPTZ NOT NULL DEFAULT now()
	)`)
	if err != nil {
		return fmt.Errorf("create schema_migrations: %w", err)
	}

	names, err := fs.Glob(fsys, "*.sql")
	if err != nil {
		return err
	}
	sort.Strings(names)

	for _, name := range names {
		var applied bool
		if err := pool.QueryRow(ctx,
			`SELECT EXISTS(SELECT 1 FROM schema_migrations WHERE version=$1)`, name,
		).Scan(&applied); err != nil {
			return err
		}
		if applied {
			continue
		}

		content, err := fs.ReadFile(fsys, name)
		if err != nil {
			return err
		}

		tx, err := pool.Begin(ctx)
		if err != nil {
			return err
		}
		if _, err := tx.Exec(ctx, string(content)); err != nil {
			_ = tx.Rollback(ctx)
			return fmt.Errorf("apply migration %s: %w", name, err)
		}
		if _, err := tx.Exec(ctx,
			`INSERT INTO schema_migrations (version) VALUES ($1)`, name,
		); err != nil {
			_ = tx.Rollback(ctx)
			return err
		}
		if err := tx.Commit(ctx); err != nil {
			return err
		}
	}
	return nil
}
