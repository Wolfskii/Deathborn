package db

import (
	"context"
	"time"
)

// WorldItemDrop is a loot pile on the ground.
type WorldItemDrop struct {
	ID                int64
	ItemID            string
	HouseID           int64
	Count             int
	X                 float64
	Y                 float64
	SourceCharacterID int64
	DroppedAt         time.Time
}

// ListWorldItemDrops returns all ground loot.
func (d *DB) ListWorldItemDrops(ctx context.Context) ([]WorldItemDrop, error) {
	rows, err := d.Pool.Query(ctx,
		`SELECT id, item_id, COALESCE(house_id, 0), count, x, y,
		        COALESCE(source_character_id, 0), dropped_at
		 FROM world_item_drops`)
	if err != nil {
		return nil, err
	}
	defer rows.Close()

	var out []WorldItemDrop
	for rows.Next() {
		var drop WorldItemDrop
		if err := rows.Scan(
			&drop.ID, &drop.ItemID, &drop.HouseID, &drop.Count, &drop.X, &drop.Y,
			&drop.SourceCharacterID, &drop.DroppedAt,
		); err != nil {
			return nil, err
		}
		out = append(out, drop)
	}
	return out, rows.Err()
}

// CreateWorldItemDrop inserts one ground loot pile.
func (d *DB) CreateWorldItemDrop(ctx context.Context, drop WorldItemDrop) (WorldItemDrop, error) {
	err := d.Pool.QueryRow(ctx,
		`INSERT INTO world_item_drops (item_id, house_id, count, x, y, source_character_id)
		 VALUES ($1, NULLIF($2, 0), $3, $4, $5, NULLIF($6, 0))
		 RETURNING id, item_id, COALESCE(house_id, 0), count, x, y,
		           COALESCE(source_character_id, 0), dropped_at`,
		drop.ItemID, drop.HouseID, drop.Count, drop.X, drop.Y, drop.SourceCharacterID,
	).Scan(
		&drop.ID, &drop.ItemID, &drop.HouseID, &drop.Count, &drop.X, &drop.Y,
		&drop.SourceCharacterID, &drop.DroppedAt,
	)
	return drop, err
}

// DeleteWorldItemDrop removes a ground loot pile.
func (d *DB) DeleteWorldItemDrop(ctx context.Context, id int64) error {
	_, err := d.Pool.Exec(ctx, `DELETE FROM world_item_drops WHERE id = $1`, id)
	return err
}
