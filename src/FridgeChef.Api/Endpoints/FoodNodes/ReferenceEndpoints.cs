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
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
                return Results.Ok(Array.Empty<FoodNodeSearchResponse>());
            return Results.Ok(await handler.HandleAsync(q, ct));
        })
        .Produces<IReadOnlyList<FoodNodeSearchResponse>>()
        .WithSummary("Поиск продуктов по названию (триграммный)");

        group.MapGet("/{id:long}", async (long id, GetFoodNodeHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(id, ct);
            return result.ToHttpResult();
        })
        .Produces<FoodNodeResponse>()
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .WithSummary("Получить продукт по ID");
    }
}
