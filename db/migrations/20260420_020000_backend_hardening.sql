CREATE UNIQUE INDEX IF NOT EXISTS uq_refresh_tokens_active_token_hash
    ON auth.refresh_tokens (token_hash)
    WHERE revoked_at IS NULL;

CREATE INDEX IF NOT EXISTS ix_refresh_tokens_active_user_id
    ON auth.refresh_tokens (user_id)
    WHERE revoked_at IS NULL;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'uq_pantry_items_user_food_node'
          AND connamespace = 'user_domain'::regnamespace
    ) THEN
        ALTER TABLE user_domain.pantry_items
            ADD CONSTRAINT uq_pantry_items_user_food_node
            UNIQUE (user_id, food_node_id);
    END IF;
END
$$;
