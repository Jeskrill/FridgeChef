using FridgeChef.Application.Favorites;
using FridgeChef.Application.Recipes.Dto;
using FridgeChef.Api.Middleware;
using Microsoft.AspNetCore.Mvc;

namespace FridgeChef.Api.Endpoints.Favorites;

internal static class FavoriteEndpoints
{
    public static void MapFavoriteEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/favorites").WithTags("Favorites").RequireAuthorization();

        group.MapGet("/", async (HttpContext http, GetFavoritesHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(http.User.GetUserId(), ct);
            return Results.Ok(result);
        })
        .Produces<IReadOnlyList<RecipeCardResponse>>()
        .WithSummary("Избранные рецепты");

        // PUT — idempotent upsert: клиент сам задаёт ключ ресурса (recipeId)
        group.MapPut("/{recipeId:guid}", async (
            Guid recipeId, HttpContext http,
            AddFavoriteHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(http.User.GetUserId(), recipeId, ct);
            return result.ToHttpResult();
        })
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .WithSummary("Добавить рецепт в избранное (идемпотентно)");

        group.MapDelete("/{recipeId:guid}", async (
            Guid recipeId, HttpContext http,
            RemoveFavoriteHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(http.User.GetUserId(), recipeId, ct);
            return result.ToHttpResult();
        })
        .Produces(StatusCodes.Status204NoContent)
        .WithSummary("Убрать рецепт из избранного");
    }
}
