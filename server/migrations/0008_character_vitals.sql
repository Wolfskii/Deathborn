-- Persist combat vitals across logout / disconnect.
ALTER TABLE characters
    ADD COLUMN IF NOT EXISTS hp DOUBLE PRECISION;

ALTER TABLE characters
    ADD COLUMN IF NOT EXISTS stamina DOUBLE PRECISION;

ALTER TABLE characters
    ADD COLUMN IF NOT EXISTS mana DOUBLE PRECISION;
