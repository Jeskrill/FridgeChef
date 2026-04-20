using FridgeChef.Application.FoodNodes;
using FridgeChef.Application.Recipes.Dto;
using FridgeChef.Api.Middleware;
using Microsoft.AspNetCore.Mvc;

namespace FridgeChef.Api.Endpoints.FoodNodes;

internal static class FoodNodeEndpoints
{
    public static void MapFoodNodeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/food-nodes").WithTags("FoodNodes");

        group.MapGet("/", async (string? q, SearchFoodNodesHandler handler, CancellationToken ct) =>
        {
            var normalizedQuery = q?.Trim();
            if (string.IsNullOrWhiteSpace(normalizedQuery) || normalizedQuery.Length < 2)
                return Results.Ok(Array.Empty<FoodNodeSearchResponse>());
            return Results.Ok(await handler.HandleAsync(normalizedQuery, ct));
        })
        .Produces<IReadOnlyList<FoodNodeSearchResponse>>()
        .WithSummary("Поиск продуктов по названию (триграммный)");

        group.MapGet("/{id:long}", async (long id, GetFoodNodeHandler handler, CancellationToken ct) =>
        {
            if (id <= 0)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["id"] = ["Food node ID must be positive."]
                });
            }

            var result = await handler.HandleAsync(id, ct);
            return result.ToHttpResult();
        })
        .Produces<FoodNodeResponse>()
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .WithSummary("Получить продукт по ID");
    }
}
