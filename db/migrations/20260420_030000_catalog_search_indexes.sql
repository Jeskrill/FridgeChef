BEGIN;

CREATE INDEX IF NOT EXISTS ix_catalog_recipes_title_trgm
    ON catalog.recipes
    USING gin (title gin_trgm_ops);

CREATE INDEX IF NOT EXISTS ix_catalog_recipes_published_created_at
    ON catalog.recipes (created_at DESC, id)
    WHERE status = 'published';

COMMIT;
