-- Living character skill progression (M3).
ALTER TABLE characters
    ADD COLUMN IF NOT EXISTS skills JSONB NOT NULL DEFAULT '{}'::jsonb;

ALTER TABLE characters
    ADD COLUMN IF NOT EXISTS total_xp BIGINT NOT NULL DEFAULT 0;
