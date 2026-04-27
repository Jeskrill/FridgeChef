using FridgeChef.Api.Middleware;
using FridgeChef.Catalog.Application.Dto;
using FridgeChef.Ontology.Application.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace FridgeChef.Api.Endpoints.FoodNodes;

internal static class FoodNodeEndpoints
{
    public static void MapFoodNodeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/food-nodes").WithTags("FoodNodes");

        group.MapGet("/", async (string? q, [FromServices] SearchFoodNodesHandler handler, CancellationToken ct) =>
        {
            var normalizedQuery = q?.Trim();
            if (string.IsNullOrWhiteSpace(normalizedQuery) || normalizedQuery.Length < 2)
                return Results.Ok(Array.Empty<FoodNodeSearchResponse>());
            return Results.Ok(await handler.HandleAsync(normalizedQuery, ct));
        })
        .Produces<IReadOnlyList<FoodNodeSearchResponse>>()
        .WithSummary("Поиск продуктов по названию")
        .WithDescription("""
            Полнотекстовый триграммный поиск продуктов в онтологии FridgeChef.
            Возвращает до 10 наиболее релевантных результатов.

            **FoodNode** — это узел в иерархии продуктов. Например:
            - «Молоко» (родитель) → «Цельное молоко», «Обезжиренное молоко» (дети)
            - «Мясо» → «Говядина» → «Стейк рибай»

            **Параметр `q`** — поисковый запрос (минимум 2 символа).
            Поиск работает по каноническим названиям и синонимам (алиасам).

            **Использование:**
            Полученный `id` используется при добавлении продукта в холодильник (`POST /pantry`).

            Этот эндпоинт открытый, авторизация не нужна.

            **Примеры запросов:**
            - `GET /food-nodes?q=курица` → найдёт «Куриное филе», «Куриная грудка» и т.д.
            - `GET /food-nodes?q=молоко` → найдёт различные виды молока
            """);

        group.MapGet("/{id:long}", async (long id, [FromServices] GetFoodNodeHandler handler, CancellationToken ct) =>
        {
            if (id <= 0)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["id"] = ["ID продукта должен быть положительным."]
                });
            }

            var result = await handler.HandleAsync(id, ct);
            return result.ToHttpResult();
        })
        .Produces<FoodNodeResponse>()
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .WithSummary("Получить продукт по ID")
        .WithDescription("""
            Возвращает полную информацию об узле онтологии по его числовому ID.

            Содержит:
            - `id` — числовой идентификатор
            - `canonicalName` — каноническое (главное) название продукта
            - `slug` — URL-метка (например `chicken-fillet`)
            - `kind` — тип узла: `Ingredient`, `Category`, `Subcategory`
            - `status` — статус: `Active`, `Deprecated`

            Этот эндпоинт открытый, авторизация не нужна.
            """);
    }
}
