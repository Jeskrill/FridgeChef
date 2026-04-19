using FridgeChef.Application.Pricing;
using FridgeChef.Pricing.Application;
using FridgeChef.Pricing.Infrastructure.Scraping;
using Microsoft.AspNetCore.Mvc;

namespace FridgeChef.Api.Endpoints.Pricing;

internal static class PricingEndpoints
{
    public static void MapPricingEndpoints(this IEndpointRouteBuilder app)
    {
        // GET /pricing/ingredients?ids=1,2,3
        app.MapGet("/pricing/ingredients", async (
            long[] ids,
            GetPricesHandler handler,
            CancellationToken ct) =>
        {
            if (ids.Length == 0) return Results.Ok(Array.Empty<IngredientPriceResponse>());
            return Results.Ok(await handler.HandleAsync(ids, ct));
        })
        .WithTags("Pricing")
        .Produces<IReadOnlyList<IngredientPriceResponse>>()
        .WithSummary("Цены на ингредиенты по food node IDs");

        // ─── Admin endpoints ───────────────────────────────────────────

        // GET /admin/pricing/status
        app.MapGet("/admin/pricing/status", async (
            PuppeteerPyaterochkaScraper scraper,
            CancellationToken ct) =>
        {
            var (ready, error) = await scraper.CheckHealthAsync(ct);
            return Results.Ok(new
            {
                scraperType = "puppeteer_sidecar",
                sidecarReady = ready,
                sidecarError = error,
                retailer = scraper.RetailerCode,
                instructions = !ready
                    ? "Start sidecar: cd tools/scraper && node server.js"
                    : "Ready. POST to /admin/pricing/sync to start.",
            });
        })
        .WithTags("Admin - Pricing")
        .WithSummary("Статус scraper sidecar");

        // POST /admin/pricing/sync
        app.MapPost("/admin/pricing/sync", async (
            PriceSyncService syncService,
            CancellationToken ct) =>
        {
            await syncService.SyncAllAsync(ct);
            return Results.Ok(new { message = "Sync completed" });
        })
        .WithTags("Admin - Pricing")
        .WithSummary("Запустить синхронизацию цен вручную");

        // POST /admin/pricing/reconnect
        app.MapPost("/admin/pricing/reconnect", async (
            PuppeteerPyaterochkaScraper scraper,
            CancellationToken ct) =>
        {
            await scraper.RestartBrowserAsync(ct);
            return Results.Ok(new { message = "Browser restart requested" });
        })
        .WithTags("Admin - Pricing")
        .WithSummary("Переподключиться к Chrome");

        // POST /admin/pricing/search-test
        app.MapPost("/admin/pricing/search-test", async (
            [FromBody] SearchTestRequest request,
            PuppeteerPyaterochkaScraper scraper,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Query))
                return Results.BadRequest("Query is required");

            var products = await scraper.SearchAsync(request.Query, ct);
            return Results.Ok(new
            {
                query = request.Query,
                count = products.Count,
                products = products.Take(5),
            });
        })
        .WithTags("Admin - Pricing")
        .WithSummary("Тестовый поиск товаров в Пятёрочке");
    }
}

public record SearchTestRequest(string Query);
