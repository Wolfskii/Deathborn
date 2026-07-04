package db

import (
	"context"
	"encoding/json"
	"errors"

	"github.com/jackc/pgx/v5"
)

// EquippedCosmetics stores worn cosmetic item ids by slot name.
type EquippedCosmetics map[string]string

func (d *DB) GetCharacterCosmetics(ctx context.Context, characterID int64) (EquippedCosmetics, error) {
	var raw []byte
	err := d.Pool.QueryRow(ctx,
		`SELECT equipped_cosmetics FROM characters WHERE id = $1`,
		characterID,
	).Scan(&raw)
	if errors.Is(err, pgx.ErrNoRows) {
		return nil, ErrNotFound
	}
	if err != nil {
		return nil, err
	}
	if len(raw) == 0 {
		return EquippedCosmetics{}, nil
	}
	var out EquippedCosmetics
	if json.Unmarshal(raw, &out) != nil {
		return EquippedCosmetics{}, nil
	}
	return out, nil
}

func (d *DB) SaveCharacterCosmetics(ctx context.Context, characterID int64, cosmetics EquippedCosmetics) error {
	if cosmetics == nil {
		cosmetics = EquippedCosmetics{}
	}
	raw, err := json.Marshal(cosmetics)
	if err != nil {
		return err
	}
	tag, err := d.Pool.Exec(ctx,
		`UPDATE characters SET equipped_cosmetics = $2 WHERE id = $1`,
		characterID, raw,
	)
	if err != nil {
		return err
	}
	if tag.RowsAffected() == 0 {
		return ErrNotFound
	}
	return nil
}
