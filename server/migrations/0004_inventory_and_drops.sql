-- Character inventory (authoritative) and ground loot from death drops.
ALTER TABLE characters
    ADD COLUMN IF NOT EXISTS inventory JSONB NOT NULL DEFAULT '[]'::jsonb;

CREATE TABLE IF NOT EXISTS world_item_drops (
    id                  BIGSERIAL PRIMARY KEY,
    item_id             TEXT NOT NULL,
    house_id            BIGINT REFERENCES houses(id) ON DELETE CASCADE,
    count               INT NOT NULL DEFAULT 1 CHECK (count > 0),
    x                   DOUBLE PRECISION NOT NULL,
    y                   DOUBLE PRECISION NOT NULL,
    source_character_id BIGINT REFERENCES characters(id) ON DELETE SET NULL,
    dropped_at          TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS world_item_drops_xy_idx ON world_item_drops (x, y);
