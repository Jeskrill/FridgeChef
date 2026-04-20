-- Кухонные предпочтения пользователя
-- Рецепты из предпочтительных кухонь получают повышенный рейтинг при подборе.
-- taxon_id ссылается на taxonomy.taxons WHERE kind = 'cuisine'

CREATE TABLE IF NOT EXISTS user_domain.user_preferred_cuisines
(
    user_id    uuid        NOT NULL REFERENCES auth.users (id) ON DELETE CASCADE,
    taxon_id   bigint      NOT NULL REFERENCES taxonomy.taxons (id) ON DELETE CASCADE,
    created_at timestamptz NOT NULL DEFAULT now(),

    PRIMARY KEY (user_id, taxon_id)
);

CREATE INDEX IF NOT EXISTS idx_user_preferred_cuisines_user_id
    ON user_domain.user_preferred_cuisines (user_id);

COMMENT ON TABLE user_domain.user_preferred_cuisines IS
    'Предпочтительные кухни пользователя. Используются при ранжировании рецептов — рецепты из этих кухонь получают бонус к рейтингу.';
