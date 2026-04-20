using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FridgeChef.Pricing.Application;

// Координирует полный цикл синхронизации цен: получает все активные ингредиенты,
// ищет каждый в ретейлере и сохраняет лучшее совпадение.
// Поддерживает два режима:
// 1. Последовательный — один запрос за раз через IRetailerScraper.SearchAsync
// 2. Пакетный — группирует запросы через IBatchRetailerScraper.SearchBatchAsync
public sealed class PriceSyncService
{
    private readonly IRetailerScraper _scraper;
    private readonly IPriceSyncRepository _repository;
    private readonly ILogger<PriceSyncService> _logger;
    private readonly PriceSyncOptions _options;

    // Максимальное количество параллельных задач скрапинга (только для последовательного режима).
    private const int MaxParallelism = 4;

    public PriceSyncService(
        IRetailerScraper scraper,
        IPriceSyncRepository repository,
        IOptions<PriceSyncOptions> options,
        ILogger<PriceSyncService> logger)
    {
        _scraper = scraper;
        _repository = repository;
        _options = options.Value;
        _logger = logger;
    }

    public async Task SyncAllAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting price sync for retailer {Retailer}", _scraper.RetailerCode);

        var retailerId = await _repository.EnsureRetailerAsync(
            _scraper.RetailerCode, "Пятёрочка", "https://5ka.ru", ct);

        var ingredients = await _repository.GetActiveIngredientsAsync(ct);
        if (_options.MaxIngredientsPerRun > 0)
        {
            ingredients = ingredients
                .Take(_options.MaxIngredientsPerRun)
                .ToList();
        }

        _logger.LogInformation("Found {Count} active ingredients to sync", ingredients.Count);

        SyncStats stats;

        if (_scraper is IBatchRetailerScraper batchScraper)
        {
            stats = await SyncBatchModeAsync(batchScraper, retailerId, ingredients, ct);
        }
        else
        {
            stats = await SyncSequentialModeAsync(retailerId, ingredients, ct);
        }

        _logger.LogInformation(
            "Price sync complete. Succeeded: {Succeeded}, Failed: {Failed}, Skipped: {Skipped}",
            stats.Succeeded, stats.Failed, stats.Skipped);
    }

    // Пакетный режим: отправляет группы запросов в скрапер.
    private async Task<SyncStats> SyncBatchModeAsync(
        IBatchRetailerScraper scraper,
        long retailerId,
        IReadOnlyList<IngredientToScrape> ingredients,
        CancellationToken ct)
    {
        var stats = new SyncStats();
        var batchSize = Math.Max(1, _options.BatchSize);

        var batches = ingredients
            .Select((ing, idx) => (ing, idx))
            .GroupBy(x => x.idx / batchSize)
            .Select(g => g.Select(x => x.ing).ToList())
            .ToList();

        _logger.LogInformation(
            "Batch mode: {Batches} batches of up to {Size} queries",
            batches.Count, batchSize);

        var batchNum = 0;
        foreach (var batch in batches)
        {
            batchNum++;
            ct.ThrowIfCancellationRequested();

            var queries = batch.Select(i => i.CanonicalName).ToList();

            _logger.LogInformation(
                "Processing batch {N}/{Total} ({Count} queries)...",
                batchNum, batches.Count, queries.Count);

            try
            {
                var results = await scraper.SearchBatchAsync(queries, ct);

                foreach (var ingredient in batch)
                {
                    if (results.TryGetValue(ingredient.CanonicalName, out var products)
                        && products.Count > 0)
                    {
                        try
                        {
                            await PersistBestMatchAsync(retailerId, ingredient, products[0], ct);
                            stats.Succeeded++;
                        }
                        catch (Exception ex)
                        {
                            stats.Failed++;
                            _logger.LogWarning(ex,
                                "DB error for {Ingredient}", ingredient.CanonicalName);
                        }
                    }
                    else
                    {
                        stats.Skipped++;
                    }
                }
            }
            catch (Exception ex)
            {
                stats.Failed += batch.Count;
                _logger.LogWarning(ex, "Batch {N} failed entirely", batchNum);
            }
        }

        return stats;
    }

    // Последовательный режим: один запрос за раз (безопасно со scoped DbContext).
    private async Task<SyncStats> SyncSequentialModeAsync(
        long retailerId,
        IReadOnlyList<IngredientToScrape> ingredients,
        CancellationToken ct)
    {
        var stats = new SyncStats();

        foreach (var ingredient in ingredients)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var products = await _scraper.SearchAsync(ingredient.CanonicalName, ct);

                if (products.Count == 0)
                {
                    _logger.LogDebug("No results for {Ingredient}", ingredient.CanonicalName);
                    stats.Skipped++;
                    continue;
                }

                await PersistBestMatchAsync(retailerId, ingredient, products[0], ct);
                stats.Succeeded++;

                _logger.LogDebug(
                    "Synced {Ingredient} → {Product} ({Price}₽)",
                    ingredient.CanonicalName, products[0].Title,
                    products[0].DiscountPrice ?? products[0].RegularPrice);
            }
            catch (Exception ex)
            {
                stats.Failed++;
                _logger.LogWarning(ex,
                    "Failed to sync price for {Ingredient} (id={Id})",
                    ingredient.CanonicalName, ingredient.FoodNodeId);
            }
        }

        return stats;
    }

    private async Task PersistBestMatchAsync(
        long retailerId, IngredientToScrape ingredient,
        ScrapedProduct best, CancellationToken ct)
    {
        await _repository.PersistBestMatchAsync(retailerId, ingredient, best, ct);
    }

    // Счётчики прогресса синхронизации.
    private sealed class SyncStats
    {
        public int Succeeded;
        public int Failed;
        public int Skipped;
    }
}
