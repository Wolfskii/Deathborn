-- Homestead farming: tilled crops and animals persist with the house plot.
ALTER TABLE houses
    ADD COLUMN IF NOT EXISTS farm JSONB NOT NULL DEFAULT '{}'::jsonb;
