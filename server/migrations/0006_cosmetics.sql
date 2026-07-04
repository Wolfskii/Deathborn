ALTER TABLE characters
    ADD COLUMN IF NOT EXISTS equipped_cosmetics JSONB NOT NULL DEFAULT '{}'::jsonb;
