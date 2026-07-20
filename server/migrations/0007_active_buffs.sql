ALTER TABLE characters
    ADD COLUMN IF NOT EXISTS active_buffs JSONB NOT NULL DEFAULT '[]'::jsonb;
