using FridgeChef.Catalog.Application.Dto;
using FridgeChef.Catalog.Application.UseCases.GetCatalog;
using FridgeChef.Catalog.Application.UseCases.GetRecipeDetail;
using FridgeChef.Catalog.Application.UseCases.MatchFromPantry;
using FridgeChef.Api.Middleware;
using FridgeChef.SharedKernel;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;

namespace FridgeChef.Api.Endpoints.Recipes;

internal static class RecipeEndpoints
{
    private const int MaxPageSize = 100;

    public static void MapRecipeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/recipes").WithTags("Recipes");

        // ── GET /recipes ─────────────────────────────────────────────────────────
        group.MapGet("/", async (
            [FromServices] GetCatalogHandler handler,
            [FromQuery(Name = "q")] string? q,
            [FromQuery(Name = "diet")] long[]? diet,
            [FromQuery(Name = "cuisine")] long[]? cuisine,
            [FromQuery(Name = "cuisineName")] string? cuisineName,
            [FromQuery(Name = "maxTime")] int? maxTime,
            [FromQuery(Name = "maxCal")] decimal? maxCal,
            [FromQuery(Name = "page")] int page = 1,
            [FromQuery(Name = "pageSize")] int pageSize = 20,
            CancellationToken ct = default) =>
        {
            var invalidDietIds    = diet?.Any(id => id <= 0) == true;
            var invalidCuisineIds = cuisine?.Any(id => id <= 0) == true;
            if (invalidDietIds || invalidCuisineIds)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["ids"] = ["Diet and cuisine IDs must be positive."]
                });
            }

            var clampedPage     = Math.Max(1, page);
            var clampedPageSize = Math.Clamp(pageSize < 1 ? 20 : pageSize, 1, MaxPageSize);

            var request = new GetCatalogRequest(
                q?.Trim(),
                diet?.Distinct().ToArray(),
                cuisine?.Distinct().ToArray(),
                cuisineName?.Trim(),
                maxTime > 0 ? maxTime : null,
                maxCal > 0 ? maxCal : null,
                clampedPage,
                clampedPageSize);

            var result = await handler.HandleAsync(request, ct);
            return Results.Ok(result);
        })
        .Produces<PagedResult<RecipeCardResponse>>()
        .WithSummary("Каталог рецептов")
        .WithDescription("""
            Постраничный список рецептов с поддержкой фильтров.

            **Фильтры:**
            - `q` — поиск по названию и ингредиентам
            - `diet` — ID диет (массив, можно передать несколько: `?diet=1&diet=2`)
            - `cuisine` — ID кухонь
            - `cuisineName` — название кухни текстом (например: `Итальянская`)
            - `maxTime` — макс. время приготовления в минутах
            - `maxCal` — макс. калорийность на порцию

            Список ID диет и кухонь: `GET /taxons`
            """);

        // ── GET /recipes/{slug} ──────────────────────────────────────────────────
        group.MapGet("/{slug}", async (
            string slug,
            [FromServices] GetRecipeDetailHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(slug, ct);
            return result.ToHttpResult();
        })
        .Produces<RecipeDetailResponse>()
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .WithSummary("Карточка рецепта")
        .WithDescription("""
            Полная информация о рецепте: название, описание, шаги, ингредиенты, КБЖУ, аллергены.

            `slug` — URL-идентификатор рецепта из поля `slug` в каталоге. Например: `borscht-classic`.
            """);

        // ── POST /recipes/search ─────────────────────────────────────────────────
        group.MapPost("/search", async (
            HttpContext http,
            [FromBody] MatchRequest request,
            IValidator<MatchRequest> validator,
            [FromServices] MatchFromPantryHandler handler,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var userId = http.User.GetUserId();
            var result = await handler.HandleAsync(userId, request, ct);
            return Results.Ok(result);
        })
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
        .RequireAuthorization()
        .Produces<IReadOnlyList<MatchResultResponse>>()
        .WithSummary("Подбор рецептов из холодильника")
        .WithDescription("""
            Подбирает рецепты на основе продуктов в холодильнике текущего пользователя.

            Алгоритм сравнивает продукты из холодильника с ингредиентами рецептов,
            учитывает иерархию продуктов, исключает аллергены, сортирует по проценту совпадения.

            **Тело запроса:**
            ```json
            {
              "dietFilterIds": [],
              "maxResults": 20
            }
            ```

            - `dietFilterIds` — фильтр по ID диет (необязательно; список: `GET /taxons?kind=Diet`)
            - `maxResults` — максимум результатов (1–100, по умолчанию 50)

            **Требует авторизации** (нужен холодильник пользователя).
            """);
    }
}
