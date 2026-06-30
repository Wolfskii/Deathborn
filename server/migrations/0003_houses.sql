-- Player housing: one plot per living character.
CREATE TABLE IF NOT EXISTS houses (
    id           BIGSERIAL PRIMARY KEY,
    character_id BIGINT NOT NULL UNIQUE REFERENCES characters(id) ON DELETE CASCADE,
    owner_name   TEXT NOT NULL,
    center_x     DOUBLE PRECISION NOT NULL,
    center_y     DOUBLE PRECISION NOT NULL,
    furniture    JSONB NOT NULL DEFAULT '[]'::jsonb,
    created_at   TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS houses_position_idx ON houses (center_x, center_y);
