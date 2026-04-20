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

        // ── Users ──────────────────────────────────────────────────────────────

        group.MapGet("/users", async (
            [FromQuery(Name = "q")] string? q,
            [FromQuery(Name = "page")] int page = 1,
            [FromQuery(Name = "pageSize")] int pageSize = 25,
            GetAdminUsersHandler handler = default!,
            CancellationToken ct = default) =>
        {
            var clampedPage     = Math.Max(1, page);
            var clampedPageSize = Math.Clamp(pageSize < 1 ? DefaultPageSize : pageSize, 1, MaxPageSize);
            var result = await handler.HandleAsync(q?.Trim(), clampedPage, clampedPageSize, ct);
            return Results.Ok(result);
        })
        .Produces<AdminUserListResponse>()
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
        .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
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
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
        .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
        .WithSummary("Заблокировать / разблокировать пользователя")
        .WithDescription("""
            Меняет статус блокировки пользователя.
            Заблокированный пользователь получает 401 при попытке войти.
            Только для администраторов.
            """);

        // ── Stats ──────────────────────────────────────────────────────────────

        group.MapGet("/stats", async (
            [FromServices] GetAdminStatsHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(ct);
            return Results.Ok(result);
        })
        .Produces<AdminStatsResponse>()
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
        .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
        .WithSummary("Агрегированная статистика платформы")
        .WithDescription("""
            Возвращает ключевые метрики: общее число пользователей, рецептов,
            добавлений в избранное, а также топ-5 самых популярных рецептов.
            Только для администраторов.
            """);
    }
}
