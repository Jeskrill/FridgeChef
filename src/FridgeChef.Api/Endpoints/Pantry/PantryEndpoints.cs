using FridgeChef.Application.Pantry;
using FridgeChef.Api.Middleware;

namespace FridgeChef.Api.Endpoints.Pantry;

internal static class PantryEndpoints
{
    public static void MapPantryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/pantry").WithTags("Pantry").RequireAuthorization();

        group.MapGet("/", async (HttpContext http, GetPantryItemsHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(http.User.GetUserId(), ct);
            return Results.Ok(result);
        });

        group.MapPost("/", async (
            HttpContext http,
            AddPantryItemRequest request,
            AddPantryItemHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(http.User.GetUserId(), request, ct);
            return result.ToHttpResult(StatusCodes.Status201Created);
        });

        group.MapPatch("/{id:guid}", async (
            Guid id, HttpContext http,
            UpdatePantryItemRequest request,
            UpdatePantryItemHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(http.User.GetUserId(), id, request, ct);
            return result.ToHttpResult();
        });

        group.MapDelete("/{id:guid}", async (
            Guid id, HttpContext http,
            RemovePantryItemHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(http.User.GetUserId(), id, ct);
            return result.ToHttpResult();
        });
    }
}
