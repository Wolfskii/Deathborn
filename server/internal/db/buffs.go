package db

import (
	"context"
	"encoding/json"
	"errors"

	"github.com/jackc/pgx/v5"
)

// SavedBuff is a paused buff snapshot (timers do not advance while offline).
type SavedBuff struct {
	ID           string  `json:"id"`
	Remain       float64 `json:"remain"`
	Duration     float64 `json:"duration"`
	MarkTargetID int64   `json:"markTargetId,omitempty"`
}

// GetCharacterActiveBuffs returns paused buffs for a character.
func (d *DB) GetCharacterActiveBuffs(ctx context.Context, characterID int64) ([]SavedBuff, error) {
	var raw []byte
	err := d.Pool.QueryRow(ctx,
		`SELECT active_buffs FROM characters WHERE id = $1`,
		characterID,
	).Scan(&raw)
	if errors.Is(err, pgx.ErrNoRows) {
		return nil, ErrNotFound
	}
	if err != nil {
		return nil, err
	}
	if len(raw) == 0 {
		return nil, nil
	}
	var out []SavedBuff
	if json.Unmarshal(raw, &out) != nil {
		return nil, nil
	}
	return out, nil
}

// SaveCharacterActiveBuffs replaces a character's paused buffs.
func (d *DB) SaveCharacterActiveBuffs(ctx context.Context, characterID int64, buffs []SavedBuff) error {
	if buffs == nil {
		buffs = []SavedBuff{}
	}
	raw, err := json.Marshal(buffs)
	if err != nil {
		return err
	}
	tag, err := d.Pool.Exec(ctx,
		`UPDATE characters SET active_buffs = $2 WHERE id = $1`,
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
