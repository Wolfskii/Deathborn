package db

import (
	"context"
	"errors"

	"github.com/jackc/pgx/v5"
)

var ErrAlreadyFriends = errors.New("already friends")
var ErrRequestExists = errors.New("friend request already sent")
var ErrNoRequest = errors.New("no friend request")

// SendFriendRequest creates a pending request from one account to another.
func (d *DB) SendFriendRequest(ctx context.Context, fromAccountID, toAccountID int64) error {
	if fromAccountID == toAccountID {
		return errors.New("cannot friend yourself")
	}
	ok, err := d.AreFriends(ctx, fromAccountID, toAccountID)
	if err != nil {
		return err
	}
	if ok {
		return ErrAlreadyFriends
	}
	_, err = d.Pool.Exec(ctx,
		`INSERT INTO friend_requests (from_account_id, to_account_id)
		 VALUES ($1, $2)
		 ON CONFLICT DO NOTHING`,
		fromAccountID, toAccountID,
	)
	if err != nil {
		return err
	}
	var exists bool
	err = d.Pool.QueryRow(ctx,
		`SELECT EXISTS(
			SELECT 1 FROM friend_requests WHERE from_account_id = $1 AND to_account_id = $2
		)`,
		fromAccountID, toAccountID,
	).Scan(&exists)
	if err != nil {
		return err
	}
	if !exists {
		return ErrRequestExists
	}
	return nil
}

// AcceptFriendRequest accepts a pending request and creates mutual friendship rows.
func (d *DB) AcceptFriendRequest(ctx context.Context, toAccountID, fromAccountID int64) error {
	tag, err := d.Pool.Exec(ctx,
		`DELETE FROM friend_requests
		 WHERE from_account_id = $1 AND to_account_id = $2`,
		fromAccountID, toAccountID,
	)
	if err != nil {
		return err
	}
	if tag.RowsAffected() == 0 {
		return ErrNoRequest
	}
	_, err = d.Pool.Exec(ctx,
		`INSERT INTO friendships (account_id, friend_account_id)
		 VALUES ($1, $2), ($2, $1)
		 ON CONFLICT DO NOTHING`,
		toAccountID, fromAccountID,
	)
	return err
}

// RemoveFriend removes mutual friendship rows.
func (d *DB) RemoveFriend(ctx context.Context, accountID, friendAccountID int64) error {
	_, err := d.Pool.Exec(ctx,
		`DELETE FROM friendships
		 WHERE (account_id = $1 AND friend_account_id = $2)
		    OR (account_id = $2 AND friend_account_id = $1)`,
		accountID, friendAccountID,
	)
	return err
}

func (d *DB) AreFriends(ctx context.Context, a, b int64) (bool, error) {
	var ok bool
	err := d.Pool.QueryRow(ctx,
		`SELECT EXISTS(
			SELECT 1 FROM friendships WHERE account_id = $1 AND friend_account_id = $2
		)`,
		a, b,
	).Scan(&ok)
	return ok, err
}

func (d *DB) ListFriendAccountIDs(ctx context.Context, accountID int64) ([]int64, error) {
	rows, err := d.Pool.Query(ctx,
		`SELECT friend_account_id FROM friendships WHERE account_id = $1 ORDER BY created_at`,
		accountID,
	)
	if err != nil {
		return nil, err
	}
	defer rows.Close()
	var ids []int64
	for rows.Next() {
		var id int64
		if err := rows.Scan(&id); err != nil {
			return ids, err
		}
		ids = append(ids, id)
	}
	return ids, rows.Err()
}

func (d *DB) ListIncomingRequests(ctx context.Context, accountID int64) ([]int64, error) {
	rows, err := d.Pool.Query(ctx,
		`SELECT from_account_id FROM friend_requests WHERE to_account_id = $1 ORDER BY created_at`,
		accountID,
	)
	if err != nil {
		return nil, err
	}
	defer rows.Close()
	var ids []int64
	for rows.Next() {
		var id int64
		if err := rows.Scan(&id); err != nil {
			return ids, err
		}
		ids = append(ids, id)
	}
	return ids, rows.Err()
}

func (d *DB) ListOutgoingRequests(ctx context.Context, accountID int64) ([]int64, error) {
	rows, err := d.Pool.Query(ctx,
		`SELECT to_account_id FROM friend_requests WHERE from_account_id = $1 ORDER BY created_at`,
		accountID,
	)
	if err != nil {
		return nil, err
	}
	defer rows.Close()
	var ids []int64
	for rows.Next() {
		var id int64
		if err := rows.Scan(&id); err != nil {
			return ids, err
		}
		ids = append(ids, id)
	}
	return ids, rows.Err()
}

func (d *DB) GetCharacterAccountID(ctx context.Context, characterID int64) (int64, error) {
	var accountID int64
	err := d.Pool.QueryRow(ctx,
		`SELECT account_id FROM characters WHERE id = $1`,
		characterID,
	).Scan(&accountID)
	if errors.Is(err, pgx.ErrNoRows) {
		return 0, ErrNotFound
	}
	return accountID, err
}

// DisplayNameForAccount returns the living character name, or the most recent dead name.
func (d *DB) DisplayNameForAccount(ctx context.Context, accountID int64) (string, error) {
	ch, err := d.GetActiveCharacter(ctx, accountID)
	if err == nil {
		return ch.Name, nil
	}
	if !errors.Is(err, ErrNotFound) {
		return "", err
	}
	var name string
	err = d.Pool.QueryRow(ctx,
		`SELECT name FROM characters
		 WHERE account_id = $1
		 ORDER BY created_at DESC
		 LIMIT 1`,
		accountID,
	).Scan(&name)
	if errors.Is(err, pgx.ErrNoRows) {
		return "Unknown", nil
	}
	return name, err
}
