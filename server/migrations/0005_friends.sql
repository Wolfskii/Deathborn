-- Account-scoped friends and incoming friend requests (survives character death).

CREATE TABLE IF NOT EXISTS friendships (
    account_id        BIGINT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
    friend_account_id BIGINT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
    created_at        TIMESTAMPTZ NOT NULL DEFAULT now(),
    PRIMARY KEY (account_id, friend_account_id),
    CHECK (account_id <> friend_account_id)
);

CREATE TABLE IF NOT EXISTS friend_requests (
    from_account_id BIGINT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
    to_account_id   BIGINT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    PRIMARY KEY (from_account_id, to_account_id),
    CHECK (from_account_id <> to_account_id)
);

CREATE INDEX IF NOT EXISTS friend_requests_to_idx ON friend_requests (to_account_id);
