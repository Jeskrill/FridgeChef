using FridgeChef.Admin.Application.UseCases;
using FridgeChef.Api.Middleware;
using Microsoft.AspNetCore.Mvc;

namespace FridgeChef.Api.Endpoints.Admin;

internal static class AdminEndpoints
{
    private const int DefaultPageSize = 25;
    private const int MaxPageSize = 100;

    public static void MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/admin")
            .WithTags("Admin")
            .RequireAuthorization("AdminOnly")
            .RequireRateLimiting("AdminPerIdentity");

        MapUserEndpoints(group);
        MapRecipeEndpoints(group);
        MapIngredientEndpoints(group);
        MapTaxonEndpoints(group);
        MapStatsEndpoints(group);
    }

    private static void MapUserEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/users", async (
            [FromQuery(Name = "q")] string? q,
            [FromQuery(Name = "page")] int page = 1,
            [FromQuery(Name = "pageSize")] int pageSize = 25,
            [FromServices] GetAdminUsersHandler handler = default!,
            CancellationToken ct = default) =>
        {
            var clampedPage     = Math.Max(1, page);
            var clampedPageSize = Math.Clamp(pageSize < 1 ? DefaultPageSize : pageSize, 1, MaxPageSize);
            var result = await handler.HandleAsync(q?.Trim(), clampedPage, clampedPageSize, ct);
            return Results.Ok(result);
        })
        .Produces<AdminUserListResponse>()
        .WithSummary("Список пользователей")
        .WithDescription("""
            Возвращает постранично список всех пользователей платформы.
            Опциональный параметр `q` фильтрует по имени или e-mail (contains, case-insensitive).
            Только для администраторов.
            """);

        group.MapPatch("/users/{userId:guid}/blocked", async (
            Guid userId,
            SetUserBlockedRequest request,
            [FromServices] SetUserBlockedHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(userId, request, ct);
            return result.ToHttpResult();
        })
        .Produces<AdminUserResponse>()
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .WithSummary("Заблокировать / разблокировать пользователя")
        .WithDescription("""
            Меняет статус блокировки пользователя.
            Заблокированный пользователь получает 401 при попытке войти.
            Только для администраторов.
            """);
    }

    private static void MapRecipeEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/recipes", async (
            [FromQuery(Name = "q")] string? q,
            [FromQuery(Name = "page")] int page = 1,
            [FromQuery(Name = "pageSize")] int pageSize = 25,
            [FromServices] GetAdminRecipesHandler handler = default!,
            CancellationToken ct = default) =>
        {
            var clampedPage     = Math.Max(1, page);
            var clampedPageSize = Math.Clamp(pageSize < 1 ? DefaultPageSize : pageSize, 1, MaxPageSize);
            return Results.Ok(await handler.HandleAsync(q?.Trim(), clampedPage, clampedPageSize, ct));
        })
        .Produces<AdminRecipeListResponse>()
        .WithSummary("Список рецептов (admin)")
        .WithDescription("""
            Постраничный список всех рецептов с поиском по названию.
            Возвращает ID, slug, title, cuisine, time, cost, status, createdAt.
            Только для администраторов.
            """);

        group.MapPatch("/recipes/{recipeId:guid}/status", async (
            Guid recipeId,
            SetRecipeStatusRequest request,
            [FromServices] UpdateRecipeStatusHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(recipeId, request, ct);
            return result.ToHttpResult();
        })
        .Produces(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .WithSummary("Изменить статус рецепта")
        .WithDescription("""
            Обновляет статус рецепта: published, draft, archived.
            Используйте archived для мягкого удаления.
            Только для администраторов.
            """);

        group.MapDelete("/recipes/{recipeId:guid}", async (
            Guid recipeId,
            [FromServices] DeleteRecipeHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(recipeId, ct);
            return result.ToHttpResult();
        })
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .WithSummary("Удалить рецепт (soft-delete)")
        .WithDescription("""
            Мягко удаляет рецепт — устанавливает статус archived.
            Рецепт остаётся в базе, но не отображается в каталоге.
            Только для администраторов.
            """);
    }

    private static void MapIngredientEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/ingredients", async (
            [FromQuery(Name = "q")] string? q,
            [FromQuery(Name = "page")] int page = 1,
            [FromQuery(Name = "pageSize")] int pageSize = 25,
            [FromServices] GetAdminIngredientsHandler handler = default!,
            CancellationToken ct = default) =>
        {
            var clampedPage     = Math.Max(1, page);
            var clampedPageSize = Math.Clamp(pageSize < 1 ? DefaultPageSize : pageSize, 1, MaxPageSize);
            return Results.Ok(await handler.HandleAsync(q?.Trim(), clampedPage, clampedPageSize, ct));
        })
        .Produces<AdminIngredientListResponse>()
        .WithSummary("Список ингредиентов (admin)")
        .WithDescription("""
            Постраничный список FoodNode с поиском по canonical_name.
            Возвращает ID, name, slug, kind, status, unit, createdAt.
            Только для администраторов.
            """);

        group.MapPost("/ingredients", async (
            CreateIngredientRequest request,
            [FromServices] CreateIngredientHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(request, ct);
            return result.ToHttpResult(StatusCodes.Status201Created);
        })
        .Produces<AdminIngredientResponse>(StatusCodes.Status201Created)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .WithSummary("Создать ингредиент")
        .WithDescription("""
            Создаёт новый FoodNode (ингредиент) в онтологии.
            Slug генерируется автоматически из canonical_name.
            Только для администраторов.
            """);

        group.MapPut("/ingredients/{ingredientId:long}", async (
            long ingredientId,
            UpdateIngredientRequest request,
            [FromServices] UpdateIngredientHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(ingredientId, request, ct);
            return result.ToHttpResult();
        })
        .Produces<AdminIngredientResponse>()
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .WithSummary("Обновить ингредиент")
        .WithDescription("""
            Частичное обновление FoodNode. Передавайте только изменяемые поля.
            Только для администраторов.
            """);

        group.MapDelete("/ingredients/{ingredientId:long}", async (
            long ingredientId,
            [FromServices] DeleteIngredientHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(ingredientId, ct);
            return result.ToHttpResult();
        })
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .WithSummary("Удалить ингредиент (soft-delete)")
        .WithDescription("""
            Мягко удаляет FoodNode — устанавливает status = deprecated.
            Только для администраторов.
            """);
    }

    private static void MapTaxonEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/taxons", async (
            [FromQuery(Name = "kind")] string? kind,
            [FromServices] GetAdminTaxonsHandler handler = default!,
            CancellationToken ct = default) =>
        {
            return Results.Ok(await handler.HandleAsync(kind?.Trim(), ct));
        })
        .Produces<AdminTaxonListResponse>()
        .WithSummary("Список таксонов (admin)")
        .WithDescription("""
            Все таксоны с фильтром по kind (Diet, Cuisine, DishType, Occasion, CookingMethod, Feature).
            Включает количество рецептов на каждый таксон.
            Только для администраторов.
            """);

        group.MapPost("/taxons", async (
            CreateTaxonRequest request,
            [FromServices] CreateTaxonHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(request, ct);
            return result.ToHttpResult(StatusCodes.Status201Created);
        })
        .Produces<AdminTaxonResponse>(StatusCodes.Status201Created)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .WithSummary("Создать таксон")
        .WithDescription("""
            Создаёт новый таксон (тег диеты, кухни, типа блюда).
            Kind — строка: Diet, Cuisine, DishType, Occasion, CookingMethod, Feature.
            Slug генерируется из name.
            Только для администраторов.
            """);

        group.MapPut("/taxons/{taxonId:long}", async (
            long taxonId,
            UpdateTaxonRequest request,
            [FromServices] UpdateTaxonHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(taxonId, request, ct);
            return result.ToHttpResult();
        })
        .Produces<AdminTaxonResponse>()
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .WithSummary("Обновить таксон")
        .WithDescription("""
            Частичное обновление таксона — name и/или description.
            Slug пересчитывается при изменении name.
            Только для администраторов.
            """);

        group.MapDelete("/taxons/{taxonId:long}", async (
            long taxonId,
            [FromServices] DeleteTaxonHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(taxonId, ct);
            return result.ToHttpResult();
        })
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .WithSummary("Удалить таксон")
        .WithDescription("""
            Удаляет таксон из системы (hard-delete).
            Связи recipe_taxons будут удалены каскадно.
            Только для администраторов.
            """);
    }

    private static void MapStatsEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/stats", async (
            [FromServices] GetAdminStatsHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(ct);
            return Results.Ok(result);
        })
        .Produces<AdminStatsResponse>()
        .WithSummary("Агрегированная статистика платформы")
        .WithDescription("""
            Возвращает ключевые метрики: общее число пользователей, рецептов,
            добавлений в избранное, а также топ-5 самых популярных рецептов.
            Только для администраторов.
            """);
    }
}
