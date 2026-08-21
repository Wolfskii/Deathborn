-- Living characters need distinct visible names. Dead characters retain their
-- history while allowing a future life to reuse the base name.
CREATE UNIQUE INDEX IF NOT EXISTS one_active_character_name
    ON characters (LOWER(name))
    WHERE alive = TRUE;
