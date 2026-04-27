using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FridgeChef.Api.Extensions;

internal sealed class TagDescriptionsDocumentFilter : IDocumentFilter
{
    private static readonly Dictionary<string, string> Descriptions = new()
    {
        ["Auth"] = "Регистрация, вход и управление сессиями.",

        ["Profile"] = "Профиль текущего пользователя: имя, e-mail, аватар, смена пароля.",

        ["Recipes"] = """
            Каталог рецептов и подбор блюд.

            - `GET /recipes` — каталог с фильтрами (диета, кухня, время, калории)
            - `GET /recipes/{slug}` — карточка рецепта с шагами и ингредиентами
            - `POST /recipes/matches` — подбор рецептов по содержимому холодильника
            """,

        ["Pantry"] = """
            Холодильник пользователя — список продуктов с количеством.
            Данные используются при подборе рецептов (`POST /recipes/matches`).
            """,

        ["Favorites"] = "Избранные рецепты пользователя.",

        ["Settings"] = """
            Персональные настройки пользователя:
            - **Аллергены** — продукты исключаются из подбора рецептов
            - **Любимые продукты** — повышают рейтинг рецептов
            - **Исключённые продукты** — вкусовые ограничения
            - **Диеты по умолчанию** — применяются при подборе автоматически
            """,

        ["FoodNodes"] = """
            База продуктов с иерархией и триграммным поиском.
            Используется для поиска продуктов при добавлении в холодильник.
            """,

        ["Reference"] = """
            Справочные данные:
            - `GET /units` — единицы измерения
            - `GET /taxons` — теги диет, кухонь, поводов для фильтрации
            """,

        ["Pricing"] = "Актуальные цены на продукты из Пятёрочки.",

        ["Admin"] = "Управление пользователями и статистика. Только для администраторов.",

        ["Admin - Pricing"] = "Управление скрапером цен Пятёрочки. Только для администраторов."
    };

    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        swaggerDoc.Tags ??= new List<OpenApiTag>();

        foreach (var (name, description) in Descriptions)
        {
            var existing = swaggerDoc.Tags.FirstOrDefault(t => t.Name == name);
            if (existing is not null)
            {
                existing.Description = description;
            }
            else
            {
                swaggerDoc.Tags.Add(new OpenApiTag { Name = name, Description = description });
            }
        }
    }
}
