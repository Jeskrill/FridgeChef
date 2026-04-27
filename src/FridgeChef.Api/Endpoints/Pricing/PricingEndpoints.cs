using FridgeChef.Pricing.Application;
using FridgeChef.Pricing.Domain;
using FridgeChef.Pricing.Infrastructure;
using FridgeChef.Pricing.Infrastructure.Scraping;
using Microsoft.AspNetCore.Mvc;

namespace FridgeChef.Api.Endpoints.Pricing;

internal static class PricingEndpoints
{
    private const int MaxIngredientIds = 100;

    public static void MapPricingEndpoints(this IEndpointRouteBuilder app)
    {

        app.MapGet("/pricing/ingredients", async (
            [FromQuery] long[] ids,
            [FromServices] GetPricesHandler handler,
            CancellationToken ct) =>
        {
            if (ids.Any(id => id <= 0))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["ids"] = ["ID ингредиентов должны быть положительными."]
                });
            }

            var distinctIds = ids.Distinct().ToArray();
            if (distinctIds.Length == 0)
                return Results.Ok(Array.Empty<IngredientPrice>());

            if (distinctIds.Length > MaxIngredientIds)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["ids"] = [$"Допускается не более {MaxIngredientIds} ID ингредиентов за запрос."]
                });
            }

            return Results.Ok(await handler.HandleAsync(distinctIds, ct));
        })
        .WithTags("Pricing")
        .Produces<IReadOnlyList<IngredientPrice>>()
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .WithSummary("Цены на ингредиенты")
        .WithDescription("""
            Возвращает актуальные цены на ингредиенты по их FoodNode ID.
            Данные парсятся из Пятёрочки через Puppeteer-sidecar и кешируются в базе.

            **Параметр `ids`** — один или несколько ID food-node (максимум 100 за запрос).
            Пример: `GET /pricing/ingredients?ids=1&ids=2&ids=5`

            Если для FoodNode цена не найдена, элемент не включается в ответ.
            Открытый эндпоинт, авторизация не нужна.
            """);

        var adminGroup = app.MapGroup("/admin/pricing")
            .WithTags("Admin - Pricing")
            .RequireAuthorization("AdminOnly")
            .RequireRateLimiting("AdminPerIdentity");

        adminGroup.MapGet("/status", async (
            [FromServices] PriceSyncRunner priceSyncRunner,
            [FromServices] IPriceSyncRepository syncRepo,
            PuppeteerPyaterochkaScraper scraper,
            CancellationToken ct) =>
        {
            var (ready, error) = await scraper.CheckHealthAsync(ct);
            var stats = await syncRepo.GetStatsAsync(ct);
            return Results.Ok(new
            {
                scraperType = "puppeteer_sidecar",
                sidecarReady = ready,
                sidecarError = error,
                syncRunning = priceSyncRunner.IsRunning,
                retailer = scraper.RetailerCode,

                updatedProductsCount = stats.UpdatedProductsCount,
                missingPricesCount = stats.MissingPricesCount,
                lastSyncAt = stats.LastSyncAt,
                instructions = !ready
                    ? "Start sidecar: cd tools/scraper && node server.js"
                    : "Готово. Отправьте POST на /admin/pricing/synchronization для запуска.",
            });
        })
        .Produces(StatusCodes.Status200OK)
        .WithSummary("Статус Puppeteer-sidecar и статистика цен")
        .WithDescription("""
            Проверяет доступность Puppeteer Node.js sidecar, который используется
            для скрапинга цен с сайта Пятёрочки (5ka.ru).

            Возвращает:
            - `sidecarReady` — доступен ли sidecar
            - `sidecarError` — причина недоступности (если есть)
            - `syncRunning` — идёт ли сейчас синхронизация
            - `updatedProductsCount` — количество позиций с актуальной ценой
            - `missingPricesCount` — количество позиций без цены
            - `lastSyncAt` — дата последней синхронизации
            - `instructions` — как запустить sidecar если он недоступен

            **Только для администраторов.**
            """);

        adminGroup.MapPost("/synchronization", async (
            [FromServices] PriceSyncRunner priceSyncRunner,
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

            return Results.Ok(new { message = "Синхронизация завершена" });
        })
        .Produces(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
        .WithSummary("Запустить синхронизацию цен вручную")
        .WithDescription("""
            Запускает полную синхронизацию цен: для каждого активного FoodNode
            ищет товар в Пятёрочке и сохраняет текущую цену в базу данных.

            Запрос блокирующий — завершится только когда синхронизация закончится.
            В production рекомендуется ScheduledJob.

            Возвращает 409 если синхронизация уже запущена.

            **Только для администраторов.**
            """);

        adminGroup.MapPost("/connection", async (
            PuppeteerPyaterochkaScraper scraper,
            CancellationToken ct) =>
        {
            await scraper.RestartBrowserAsync(ct);
            return Results.Ok(new { message = "Перезапуск браузера запрошен" });
        })
        .Produces(StatusCodes.Status200OK)
        .WithSummary("Переподключиться к Chrome")
        .WithDescription("""
            Перезапускает соединение с Chrome Remote Debugging в Puppeteer sidecar.
            Используйте если scraper завис или потерял сессию (например после блокировки WAF Пятёрочки).
            **Только для администраторов.**
            """);

        adminGroup.MapPost("/test-queries", async (
            [FromBody] SearchTestRequest request,
            PuppeteerPyaterochkaScraper scraper,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Query))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["query"] = ["Поисковый запрос обязателен."]
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
        .Produces(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .WithSummary("Тестовый поиск товаров в Пятёрочке")
        .WithDescription("""
            Выполняет тестовый поиск товара по названию на 5ka.ru через Puppeteer sidecar.
            Возвращает первые 5 найденных товаров с ценами.
            Используется для диагностики маппинга FoodNode → SKU Пятёрочки.

            **Тело запроса:**
            - `query` — поисковый запрос (например `"Молоко 3.2%"`)

            **Только для администраторов.**
            """);
    }
}

internal sealed record SearchTestRequest(string Query);
