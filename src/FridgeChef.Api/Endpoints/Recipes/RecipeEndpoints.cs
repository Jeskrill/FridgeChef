using FridgeChef.Application.Recipes.Dto;
using FridgeChef.Application.Recipes.GetCatalog;
using FridgeChef.Application.Recipes.GetRecipeDetail;
using FridgeChef.Application.Recipes.MatchFromPantry;
using FridgeChef.Api.Middleware;
using FridgeChef.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace FridgeChef.Api.Endpoints.Recipes;

internal static class RecipeEndpoints
{
    private const int MaxPageSize = 100;

    public static void MapRecipeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/recipes").WithTags("Recipes");

        group.MapGet("/", async (
            string? q, long[]? diet, long[]? cuisine, int page, int pageSize,
            GetCatalogHandler handler, CancellationToken ct) =>
        {
            var clampedPage     = Math.Max(1, page);
            var clampedPageSize = Math.Clamp(pageSize < 1 ? 20 : pageSize, 1, MaxPageSize);
            var request = new GetCatalogRequest(q, diet, cuisine, clampedPage, clampedPageSize);
            var result = await handler.HandleAsync(request, ct);
            return Results.Ok(result);
        })
        .Produces<PagedResult<RecipeCardResponse>>()
        .WithSummary("Каталог рецептов");

        group.MapGet("/{slug}", async (
            string slug,
            GetRecipeDetailHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(slug, ct);
            return result.ToHttpResult();
        })
        .Produces<RecipeDetailResponse>()
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .WithSummary("Детальная страница рецепта");

        // POST /search is intentional: it's a non-REST action endpoint for complex querying
        // (needs body for filter params). Not idempotent since pantry state changes affect results.
        group.MapPost("/search", async (
            HttpContext http,
            MatchRequest request,
            MatchHandler handler,
            CancellationToken ct) =>
        {
            var userId = http.User.GetUserId();
            var result = await handler.HandleAsync(userId, request, ct);
            return Results.Ok(result);
        })
        .RequireAuthorization()
        .Produces<IReadOnlyList<MatchResultResponse>>()
        .WithSummary("Подбор рецептов по продуктам из холодильника");
    }
}
