-- Accounts: permanent identity, separate from characters.
-- password_hash is nullable so OAuth-only accounts can be added later
-- without a schema migration.
CREATE TABLE IF NOT EXISTS accounts (
    id            BIGSERIAL PRIMARY KEY,
    email         TEXT NOT NULL UNIQUE,
    password_hash TEXT,
    created_at    TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- Linked OAuth identities (Google initially, extensible later).
-- Created now but unused in M0 so adding OAuth needs no migration.
CREATE TABLE IF NOT EXISTS oauth_identities (
    id               BIGSERIAL PRIMARY KEY,
    account_id       BIGINT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
    provider         TEXT NOT NULL,
    provider_user_id TEXT NOT NULL,
    created_at       TIMESTAMPTZ NOT NULL DEFAULT now(),
    UNIQUE (provider, provider_user_id)
);

-- Characters: a single life. Deleted permanently on death (later milestone).
CREATE TABLE IF NOT EXISTS characters (
    id         BIGSERIAL PRIMARY KEY,
    account_id BIGINT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
    name       TEXT NOT NULL,
    alive      BOOLEAN NOT NULL DEFAULT TRUE,
    pos_x      DOUBLE PRECISION NOT NULL DEFAULT 0,
    pos_y      DOUBLE PRECISION NOT NULL DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- Enforce at most one living character per account.
CREATE UNIQUE INDEX IF NOT EXISTS one_active_character_per_account
    ON characters (account_id) WHERE alive;

-- Permanent legacy record written when a character dies (later milestone).
CREATE TABLE IF NOT EXISTS character_history (
    id               BIGSERIAL PRIMARY KEY,
    account_id       BIGINT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
    name             TEXT NOT NULL,
    total_xp         BIGINT NOT NULL DEFAULT 0,
    skills           JSONB NOT NULL DEFAULT '{}'::jsonb,
    survival_seconds BIGINT NOT NULL DEFAULT 0,
    pvp_kills        INTEGER NOT NULL DEFAULT 0,
    wealth           BIGINT NOT NULL DEFAULT 0,
    died_at          TIMESTAMPTZ NOT NULL DEFAULT now(),
    death_reason     TEXT NOT NULL DEFAULT ''
);

-- Per-account lifetime competitive stats (later milestone).
CREATE TABLE IF NOT EXISTS highscores (
    account_id               BIGINT PRIMARY KEY REFERENCES accounts(id) ON DELETE CASCADE,
    longest_survival_seconds BIGINT NOT NULL DEFAULT 0,
    highest_total_xp         BIGINT NOT NULL DEFAULT 0,
    most_pvp_kills           INTEGER NOT NULL DEFAULT 0,
    highest_wealth           BIGINT NOT NULL DEFAULT 0,
    highest_skill_level      INTEGER NOT NULL DEFAULT 0,
    updated_at               TIMESTAMPTZ NOT NULL DEFAULT now()
);
