-- Death records: PvP kill counter, killer name and epitaph on each past life,
-- and indexes for the history and leaderboard queries.
ALTER TABLE characters ADD COLUMN IF NOT EXISTS pvp_kills INTEGER NOT NULL DEFAULT 0;
ALTER TABLE character_history ADD COLUMN IF NOT EXISTS killer_name TEXT NOT NULL DEFAULT '';
ALTER TABLE character_history ADD COLUMN IF NOT EXISTS epitaph TEXT NOT NULL DEFAULT '';

CREATE INDEX IF NOT EXISTS character_history_account_died_idx
    ON character_history (account_id, died_at DESC);
CREATE INDEX IF NOT EXISTS character_history_died_idx
    ON character_history (died_at DESC);
