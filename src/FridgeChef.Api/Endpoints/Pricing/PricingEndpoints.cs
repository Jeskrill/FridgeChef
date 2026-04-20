using FridgeChef.Application.Pricing;
using FridgeChef.Pricing.Application;
using FridgeChef.Pricing.Infrastructure.Scraping;
using FridgeChef.Pricing.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace FridgeChef.Api.Endpoints.Pricing;

internal static class PricingEndpoints
{
    private const int MaxIngredientIds = 100;

    public static void MapPricingEndpoints(this IEndpointRouteBuilder app)
    {
        // GET /pricing/ingredients?ids=1,2,3
        app.MapGet("/pricing/ingredients", async (
            long[] ids,
            GetPricesHandler handler,
            CancellationToken ct) =>
        {
            if (ids.Any(id => id <= 0))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["ids"] = ["Ingredient IDs must be positive."]
                });
            }

            var distinctIds = ids.Distinct().ToArray();
            if (distinctIds.Length == 0)
                return Results.Ok(Array.Empty<IngredientPriceResponse>());

            if (distinctIds.Length > MaxIngredientIds)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["ids"] = [$"No more than {MaxIngredientIds} ingredient IDs are allowed per request."]
                });
            }

            return Results.Ok(await handler.HandleAsync(distinctIds, ct));
        })
        .WithTags("Pricing")
        .Produces<IReadOnlyList<IngredientPriceResponse>>()
        .WithSummary("Цены на ингредиенты по food node IDs");

        // ─── Admin endpoints ───────────────────────────────────────────
        var adminGroup = app.MapGroup("/admin/pricing")
            .WithTags("Admin - Pricing")
            .RequireAuthorization("AdminOnly")
            .RequireRateLimiting("AdminPerIdentity");

        // GET /admin/pricing/status
        adminGroup.MapGet("/status", async (
            PriceSyncRunner priceSyncRunner,
            PuppeteerPyaterochkaScraper scraper,
            CancellationToken ct) =>
        {
            var (ready, error) = await scraper.CheckHealthAsync(ct);
            return Results.Ok(new
            {
                scraperType = "puppeteer_sidecar",
                sidecarReady = ready,
                sidecarError = error,
                syncRunning = priceSyncRunner.IsRunning,
                retailer = scraper.RetailerCode,
                instructions = !ready
                    ? "Start sidecar: cd tools/scraper && node server.js"
                    : "Ready. POST to /admin/pricing/sync to start.",
            });
        })
        .WithSummary("Статус scraper sidecar");

        // POST /admin/pricing/sync
        adminGroup.MapPost("/sync", async (
            PriceSyncRunner priceSyncRunner,
            CancellationToken ct) =>
        {
            var started = await priceSyncRunner.TryRunAsync(ct);
            if (!started)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Синхронизация уже выполняется",
                    detail: "Дождитесь завершения текущего запуска и повторите запрос позже.");
            }

            return Results.Ok(new { message = "Sync completed" });
        })
        .WithSummary("Запустить синхронизацию цен вручную");

        // POST /admin/pricing/reconnect
        adminGroup.MapPost("/reconnect", async (
            PuppeteerPyaterochkaScraper scraper,
            CancellationToken ct) =>
        {
            await scraper.RestartBrowserAsync(ct);
            return Results.Ok(new { message = "Browser restart requested" });
        })
        .WithSummary("Переподключиться к Chrome");

        // POST /admin/pricing/search-test
        adminGroup.MapPost("/search-test", async (
            [FromBody] SearchTestRequest request,
            PuppeteerPyaterochkaScraper scraper,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Query))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["query"] = ["Query is required."]
                });
            }

            var products = await scraper.SearchAsync(request.Query.Trim(), ct);
            return Results.Ok(new
            {
                query = request.Query.Trim(),
                count = products.Count,
                products = products.Take(5),
            });
        })
        .WithSummary("Тестовый поиск товаров в Пятёрочке");
    }
}

public record SearchTestRequest(string Query);
