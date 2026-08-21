-- Character identity and visual appearance chosen during character creation.
ALTER TABLE characters
    ADD COLUMN IF NOT EXISTS race TEXT NOT NULL DEFAULT 'human';

ALTER TABLE characters
    ADD COLUMN IF NOT EXISTS appearance JSONB NOT NULL DEFAULT '{}'::jsonb;
