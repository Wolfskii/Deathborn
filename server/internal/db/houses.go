package db

import (
	"context"
	"encoding/json"
	"errors"

	"github.com/jackc/pgx/v5"
	"github.com/jackc/pgx/v5/pgconn"
)

var ErrHouseExists = errors.New("character already has a house")

// FurnitureItem is a placed object inside a house interior.
type FurnitureItem struct {
	Type string  `json:"type"`
	X    float64 `json:"x"`
	Y    float64 `json:"y"`
}

// House is a player-owned safe plot.
type House struct {
	ID          int64
	CharacterID int64
	OwnerName   string
	CenterX     float64
	CenterY     float64
	Furniture   []FurnitureItem
}

// ListHouses returns all houses (including those owned by dead characters until key transfer).
func (d *DB) ListHouses(ctx context.Context) ([]House, error) {
	rows, err := d.Pool.Query(ctx,
		`SELECT id, character_id, owner_name, center_x, center_y, furniture FROM houses`)
	if err != nil {
		return nil, err
	}
	defer rows.Close()

	var out []House
	for rows.Next() {
		var h House
		var furnitureJSON []byte
		if err := rows.Scan(&h.ID, &h.CharacterID, &h.OwnerName, &h.CenterX, &h.CenterY, &furnitureJSON); err != nil {
			return nil, err
		}
		h.Furniture = decodeFurniture(furnitureJSON)
		out = append(out, h)
	}
	return out, rows.Err()
}

// GetHouseByCharacter returns the house owned by a character, or ErrNotFound.
func (d *DB) GetHouseByCharacter(ctx context.Context, characterID int64) (House, error) {
	var h House
	var furnitureJSON []byte
	err := d.Pool.QueryRow(ctx,
		`SELECT id, character_id, owner_name, center_x, center_y, furniture
		 FROM houses WHERE character_id = $1`,
		characterID,
	).Scan(&h.ID, &h.CharacterID, &h.OwnerName, &h.CenterX, &h.CenterY, &furnitureJSON)
	if errors.Is(err, pgx.ErrNoRows) {
		return House{}, ErrNotFound
	}
	if err != nil {
		return House{}, err
	}
	h.Furniture = decodeFurniture(furnitureJSON)
	return h, nil
}

// CreateHouse inserts a new house for a character.
func (d *DB) CreateHouse(ctx context.Context, characterID int64, ownerName string, x, y float64) (House, error) {
	var h House
	var furnitureJSON []byte
	err := d.Pool.QueryRow(ctx,
		`INSERT INTO houses (character_id, owner_name, center_x, center_y)
		 VALUES ($1, $2, $3, $4)
		 RETURNING id, character_id, owner_name, center_x, center_y, furniture`,
		characterID, ownerName, x, y,
	).Scan(&h.ID, &h.CharacterID, &h.OwnerName, &h.CenterX, &h.CenterY, &furnitureJSON)
	if err != nil {
		var pgErr *pgconn.PgError
		if errors.As(err, &pgErr) && pgErr.Code == "23505" {
			return House{}, ErrHouseExists
		}
		return House{}, err
	}
	h.Furniture = decodeFurniture(furnitureJSON)
	return h, nil
}

// SaveHouseFurniture updates furniture layout for a house.
func (d *DB) SaveHouseFurniture(ctx context.Context, characterID int64, items []FurnitureItem) error {
	raw, err := json.Marshal(items)
	if err != nil {
		return err
	}
	tag, err := d.Pool.Exec(ctx,
		`UPDATE houses SET furniture = $2 WHERE character_id = $1`,
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

// DeleteHouseByCharacter removes a character's house.
func (d *DB) DeleteHouseByCharacter(ctx context.Context, characterID int64) error {
	_, err := d.Pool.Exec(ctx, `DELETE FROM houses WHERE character_id = $1`, characterID)
	return err
}

// DeleteHouse removes a house by id.
func (d *DB) DeleteHouse(ctx context.Context, houseID int64) error {
	_, err := d.Pool.Exec(ctx, `DELETE FROM houses WHERE id = $1`, houseID)
	return err
}

// TransferHouse updates ownership to a new living character.
func (d *DB) TransferHouse(ctx context.Context, houseID, newCharacterID int64, newOwnerName string) (House, error) {
	var h House
	var furnitureJSON []byte
	err := d.Pool.QueryRow(ctx,
		`UPDATE houses SET character_id = $2, owner_name = $3
		 WHERE id = $1
		 RETURNING id, character_id, owner_name, center_x, center_y, furniture`,
		houseID, newCharacterID, newOwnerName,
	).Scan(&h.ID, &h.CharacterID, &h.OwnerName, &h.CenterX, &h.CenterY, &furnitureJSON)
	if errors.Is(err, pgx.ErrNoRows) {
		return House{}, ErrNotFound
	}
	if err != nil {
		return House{}, err
	}
	h.Furniture = decodeFurniture(furnitureJSON)
	return h, nil
}

func decodeFurniture(raw []byte) []FurnitureItem {
	if len(raw) == 0 {
		return nil
	}
	var items []FurnitureItem
	if json.Unmarshal(raw, &items) != nil {
		return nil
	}
	return items
}
