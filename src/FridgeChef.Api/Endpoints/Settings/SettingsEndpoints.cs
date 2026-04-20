using FridgeChef.Application.Settings;
using FridgeChef.Api.Middleware;
using FluentValidation;

namespace FridgeChef.Api.Endpoints.Settings;

internal static class SettingsEndpoints
{
    public static void MapSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/settings").WithTags("Settings").RequireAuthorization();

        // Allergens
        group.MapGet("/allergens", async (HttpContext http, GetAllergensHandler handler, CancellationToken ct) =>
            Results.Ok(await handler.HandleAsync(http.User.GetUserId(), ct)));

        group.MapPost("/allergens", async (
            HttpContext http,
            AddAllergenRequest request,
            IValidator<AddAllergenRequest> validator,
            AddAllergenHandler handler,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var result = await handler.HandleAsync(http.User.GetUserId(), request, ct);
            return result.ToHttpResult();
        });

        group.MapDelete("/allergens/{foodNodeId:long}", async (long foodNodeId, HttpContext http, RemoveAllergenHandler handler, CancellationToken ct) =>
        {
            if (foodNodeId <= 0)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["foodNodeId"] = ["Food node ID must be positive."]
                });
            }

            var result = await handler.HandleAsync(http.User.GetUserId(), foodNodeId, ct);
            return result.ToHttpResult();
        });

        // Favorite Foods
        group.MapGet("/favorite-foods", async (HttpContext http, GetFavoriteFoodsHandler handler, CancellationToken ct) =>
            Results.Ok(await handler.HandleAsync(http.User.GetUserId(), ct)));

        group.MapPost("/favorite-foods", async (
            HttpContext http,
            AddFavoriteFoodRequest request,
            IValidator<AddFavoriteFoodRequest> validator,
            AddFavoriteFoodHandler handler,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var result = await handler.HandleAsync(http.User.GetUserId(), request, ct);
            return result.ToHttpResult();
        });

        group.MapDelete("/favorite-foods/{foodNodeId:long}", async (long foodNodeId, HttpContext http, RemoveFavoriteFoodHandler handler, CancellationToken ct) =>
        {
            if (foodNodeId <= 0)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["foodNodeId"] = ["Food node ID must be positive."]
                });
            }

            var result = await handler.HandleAsync(http.User.GetUserId(), foodNodeId, ct);
            return result.ToHttpResult();
        });

        // Excluded Foods
        group.MapGet("/excluded-foods", async (HttpContext http, GetExcludedFoodsHandler handler, CancellationToken ct) =>
            Results.Ok(await handler.HandleAsync(http.User.GetUserId(), ct)));

        group.MapPost("/excluded-foods", async (
            HttpContext http,
            AddExcludedFoodRequest request,
            IValidator<AddExcludedFoodRequest> validator,
            AddExcludedFoodHandler handler,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var result = await handler.HandleAsync(http.User.GetUserId(), request, ct);
            return result.ToHttpResult();
        });

        group.MapDelete("/excluded-foods/{foodNodeId:long}", async (long foodNodeId, HttpContext http, RemoveExcludedFoodHandler handler, CancellationToken ct) =>
        {
            if (foodNodeId <= 0)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["foodNodeId"] = ["Food node ID must be positive."]
                });
            }

            var result = await handler.HandleAsync(http.User.GetUserId(), foodNodeId, ct);
            return result.ToHttpResult();
        });

        // Diets
        group.MapGet("/diets", async (HttpContext http, GetDietsHandler handler, CancellationToken ct) =>
            Results.Ok(await handler.HandleAsync(http.User.GetUserId(), ct)));

        group.MapPut("/diets", async (
            HttpContext http,
            UpdateDietsRequest request,
            IValidator<UpdateDietsRequest> validator,
            UpdateDietsHandler handler,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var result = await handler.HandleAsync(http.User.GetUserId(), request, ct);
            return result.ToHttpResult();
        });
    }
}
