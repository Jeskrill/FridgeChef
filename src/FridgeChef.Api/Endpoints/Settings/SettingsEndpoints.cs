using FridgeChef.UserPreferences.Application.UseCases;
using FridgeChef.Api.Middleware;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace FridgeChef.Api.Endpoints.Settings;

internal static class SettingsEndpoints
{
    public static void MapSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/settings")
            .WithTags("Settings")
            .RequireAuthorization();

        // ── Allergens ───────────────────────────────────────────────────────────

        group.MapGet("/allergens", async (HttpContext http, [FromServices] GetAllergensHandler handler, CancellationToken ct) =>
            Results.Ok(await handler.HandleAsync(http.User.GetUserId(), ct)))
        .Produces<IReadOnlyList<AllergenResponse>>()
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
        .WithSummary("Аллергены пользователя")
        .WithDescription("""
            Возвращает список аллергенов пользователя.
            Рецепты, содержащие эти продукты, исключаются из результатов подбора.
            Требуется JWT-авторизация.
            """);

        group.MapPost("/allergens", async (
            HttpContext http,
            AddAllergenRequest request,
            IValidator<AddAllergenRequest> validator,
            [FromServices] AddAllergenHandler handler,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var result = await handler.HandleAsync(http.User.GetUserId(), request, ct);
            return result.ToHttpResult();
        })
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
        .WithSummary("Добавить аллерген")
        .WithDescription("""
            Добавляет продукт в список аллергенов.
            - `foodNodeId` — ID продукта из онтологии (`GET /food-nodes?q=...`)
            - `severity` — `strict` (полное исключение) или `mild` (предупреждение)

            Требуется JWT-авторизация.
            """);

        group.MapDelete("/allergens/{foodNodeId:long}", async (
            long foodNodeId, HttpContext http, [FromServices] RemoveAllergenHandler handler, CancellationToken ct) =>
        {
            if (foodNodeId <= 0)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                    { ["foodNodeId"] = ["ID должен быть положительным."] });

            var result = await handler.HandleAsync(http.User.GetUserId(), foodNodeId, ct);
            return result.ToHttpResult();
        })
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
        .WithSummary("Удалить аллерген")
        .WithDescription("Удаляет продукт из списка аллергенов. Требуется JWT-авторизация.");

        // ── Favorite Foods ──────────────────────────────────────────────────────

        group.MapGet("/favorite-foods", async (HttpContext http, [FromServices] GetFavoriteFoodsHandler handler, CancellationToken ct) =>
            Results.Ok(await handler.HandleAsync(http.User.GetUserId(), ct)))
        .Produces<IReadOnlyList<FavoriteFoodResponse>>()
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
        .WithSummary("Любимые продукты")
        .WithDescription("""
            Возвращает список любимых продуктов пользователя.
            Рецепты с этими продуктами получают повышенный рейтинг при подборе.
            Требуется JWT-авторизация.
            """);

        group.MapPost("/favorite-foods", async (
            HttpContext http,
            AddFavoriteFoodRequest request,
            IValidator<AddFavoriteFoodRequest> validator,
            [FromServices] AddFavoriteFoodHandler handler,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var result = await handler.HandleAsync(http.User.GetUserId(), request, ct);
            return result.ToHttpResult();
        })
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
        .WithSummary("Добавить любимый продукт")
        .WithDescription("Добавляет продукт в список любимых. Требуется JWT-авторизация.");

        group.MapDelete("/favorite-foods/{foodNodeId:long}", async (
            long foodNodeId, HttpContext http, [FromServices] RemoveFavoriteFoodHandler handler, CancellationToken ct) =>
        {
            if (foodNodeId <= 0)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                    { ["foodNodeId"] = ["ID должен быть положительным."] });

            var result = await handler.HandleAsync(http.User.GetUserId(), foodNodeId, ct);
            return result.ToHttpResult();
        })
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
        .WithSummary("Удалить любимый продукт")
        .WithDescription("Удаляет продукт из списка любимых. Требуется JWT-авторизация.");

        // ── Excluded Foods ──────────────────────────────────────────────────────

        group.MapGet("/excluded-foods", async (HttpContext http, [FromServices] GetExcludedFoodsHandler handler, CancellationToken ct) =>
            Results.Ok(await handler.HandleAsync(http.User.GetUserId(), ct)))
        .Produces<IReadOnlyList<ExcludedFoodResponse>>()
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
        .WithSummary("Исключённые продукты")
        .WithDescription("""
            Возвращает список продуктов, которые пользователь не хочет видеть в рецептах.
            Отличается от аллергенов: не исключает рецепты, а понижает их рейтинг.
            Требуется JWT-авторизация.
            """);

        group.MapPost("/excluded-foods", async (
            HttpContext http,
            AddExcludedFoodRequest request,
            IValidator<AddExcludedFoodRequest> validator,
            [FromServices] AddExcludedFoodHandler handler,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var result = await handler.HandleAsync(http.User.GetUserId(), request, ct);
            return result.ToHttpResult();
        })
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
        .WithSummary("Добавить исключённый продукт")
        .WithDescription("Добавляет продукт в список нежелательных. Требуется JWT-авторизация.");

        group.MapDelete("/excluded-foods/{foodNodeId:long}", async (
            long foodNodeId, HttpContext http, [FromServices] RemoveExcludedFoodHandler handler, CancellationToken ct) =>
        {
            if (foodNodeId <= 0)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                    { ["foodNodeId"] = ["ID должен быть положительным."] });

            var result = await handler.HandleAsync(http.User.GetUserId(), foodNodeId, ct);
            return result.ToHttpResult();
        })
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
        .WithSummary("Удалить продукт из исключений")
        .WithDescription("Удаляет продукт из списка нежелательных. Требуется JWT-авторизация.");

        // ── Diets ───────────────────────────────────────────────────────────────

        group.MapGet("/diets", async (HttpContext http, [FromServices] GetDietsHandler handler, CancellationToken ct) =>
            Results.Ok(await handler.HandleAsync(http.User.GetUserId(), ct)))
        .Produces<IReadOnlyList<UserDietResponse>>()
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
        .WithSummary("Диеты по умолчанию")
        .WithDescription("""
            Возвращает диетические предпочтения пользователя, применяемые при подборе рецептов.
            Список доступных диет: `GET /taxons?kind=Diet`.
            Требуется JWT-авторизация.
            """);

        group.MapPut("/diets", async (
            HttpContext http,
            UpdateDietsRequest request,
            IValidator<UpdateDietsRequest> validator,
            [FromServices] UpdateDietsHandler handler,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var result = await handler.HandleAsync(http.User.GetUserId(), request, ct);
            return result.ToHttpResult();
        })
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
        .WithSummary("Обновить диеты по умолчанию")
        .WithDescription("""
            Полностью заменяет список диетических предпочтений.
            Пустой массив `taxonIds: []` сбрасывает все диеты.
            Максимум 50 уникальных ID. Список ID: `GET /taxons?kind=Diet`.
            Требуется JWT-авторизация.
            """);

        // ── Cuisines ────────────────────────────────────────────────────────────

        group.MapGet("/cuisines", async (HttpContext http, [FromServices] GetCuisinesHandler handler, CancellationToken ct) =>
            Results.Ok(await handler.HandleAsync(http.User.GetUserId(), ct)))
        .Produces<IReadOnlyList<UserCuisineResponse>>()
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
        .WithSummary("Предпочтительные кухни")
        .WithDescription("""
            Возвращает список nationally кухонь, предпочтительных для пользователя.
            Рецепты из этих кухонь получают повышенный рейтинг при подборе и отображаются первыми.
            Список доступных кухонь: `GET /taxons?kind=Cuisine`.
            Требуется JWT-авторизация.
            """);

        group.MapPut("/cuisines", async (
            HttpContext http,
            UpdateCuisinesRequest request,
            IValidator<UpdateCuisinesRequest> validator,
            [FromServices] UpdateCuisinesHandler handler,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var result = await handler.HandleAsync(http.User.GetUserId(), request, ct);
            return result.ToHttpResult();
        })
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
        .WithSummary("Обновить предпочтительные кухни")
        .WithDescription("""
            Полностью заменяет список предпочтительных кухонь.
            Пустой массив `taxonIds: []` сбрасывает все предпочтения.
            Максимум 20 уникальных ID. Список ID кухонь: `GET /taxons?kind=Cuisine`.
            Требуется JWT-авторизация.
            """);
    }
}
