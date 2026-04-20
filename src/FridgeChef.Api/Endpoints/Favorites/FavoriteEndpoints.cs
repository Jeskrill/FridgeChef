using FridgeChef.Favorites.Application.UseCases;
using FridgeChef.Catalog.Application.Dto;
using FridgeChef.Api.Middleware;
using Microsoft.AspNetCore.Mvc;

namespace FridgeChef.Api.Endpoints.Favorites;

internal static class FavoriteEndpoints
{
    public static void MapFavoriteEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/favorites")
            .WithTags("Favorites")
            .RequireAuthorization();

        // ── GET /favorites ──────────────────────────────────────────────────────
        group.MapGet("/", async (HttpContext http, [FromServices] GetFavoritesHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(http.User.GetUserId(), ct);
            return Results.Ok(result);
        })
        .Produces<IReadOnlyList<RecipeCardResponse>>()
        .WithSummary("Избранные рецепты")
        .WithDescription("""
            Возвращает список рецептов, добавленных текущим пользователем в избранное,
            в виде карточек (RecipeCard). Отсортированы по дате добавления (новые первые).

            **Требует JWT-авторизацию.**
            """);

        // ── PUT /favorites/{recipeId} ───────────────────────────────────────────
        // PUT — idempotent upsert: клиент сам задаёт ключ ресурса (recipeId)
        group.MapPut("/{recipeId:guid}", async (
            Guid recipeId, HttpContext http,
            [FromServices] AddFavoriteHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(http.User.GetUserId(), recipeId, ct);
            return result.ToHttpResult();
        })
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .WithSummary("Добавить рецепт в избранное")
        .WithDescription("""
            Идемпотентно добавляет рецепт в избранное текущего пользователя.
            Используется метод PUT (не POST), поскольку клиент задаёт ключ ресурса (recipeId).
            Повторный вызов с тем же recipeId — не ошибка, вернёт 204.

            Возвращает 404 если рецепт с таким ID не существует.

            **Требует JWT-авторизацию.**
            """);

        // ── DELETE /favorites/{recipeId} ────────────────────────────────────────
        group.MapDelete("/{recipeId:guid}", async (
            Guid recipeId, HttpContext http,
            [FromServices] RemoveFavoriteHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(http.User.GetUserId(), recipeId, ct);
            return result.ToHttpResult();
        })
        .Produces(StatusCodes.Status204NoContent)
        .WithSummary("Убрать рецепт из избранного")
        .WithDescription("""
            Удаляет рецепт из избранного. Если рецепт не был в избранном — реакция 204 (идемпотентно).

            **Требует JWT-авторизацию.**
            """);
    }
}
