using FluentAssertions;
using FridgeChef.Pricing.Application;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FridgeChef.Backend.Tests.Pricing;

public sealed class PriceSyncServiceTests
{
    [Fact]
    public async Task SyncAllAsync_ShouldRespectConfiguredIngredientLimit()
    {
        var scraper = new FakeBatchScraper();
        var repository = new RecordingPriceSyncRepository();
        var handler = new PriceSyncService(
            scraper,
            repository,
            Options.Create(new PriceSyncOptions
            {
                BatchSize = 2,
                MaxIngredientsPerRun = 3
            }),
            NullLogger<PriceSyncService>.Instance);

        await handler.SyncAllAsync(CancellationToken.None);

        repository.PersistedMatches.Should().HaveCount(3);
        repository.PersistedMatches.Select(item => item.Ingredient.FoodNodeId)
            .Should()
            .Equal(15, 16, 17);
    }

    private sealed class FakeBatchScraper : IBatchRetailerScraper
    {
        public string RetailerCode => "pyaterochka";

        public Task<IReadOnlyList<ScrapedProduct>> SearchAsync(string query, CancellationToken ct)
        {
            IReadOnlyList<ScrapedProduct> products =
            [
                CreateProduct(query)
            ];
            return Task.FromResult(products);
        }

        public Task<Dictionary<string, IReadOnlyList<ScrapedProduct>>> SearchBatchAsync(
            IReadOnlyList<string> queries,
            CancellationToken ct)
        {
            var results = queries.ToDictionary(
                query => query,
                query => (IReadOnlyList<ScrapedProduct>)
                [
                    CreateProduct(query)
                ]);

            return Task.FromResult(results);
        }

        private static ScrapedProduct CreateProduct(string query)
        {
            return new ScrapedProduct(
                $"sku-{query}",
                $"{query} product",
                "brand",
                199,
                149,
                $"https://example.test/{query}");
        }
    }

    private sealed class RecordingPriceSyncRepository : IPriceSyncRepository
    {
        public List<(long RetailerId, IngredientToScrape Ingredient, ScrapedProduct Product)> PersistedMatches { get; } = [];

        public Task<long> EnsureRetailerAsync(string code, string name, string baseUrl, CancellationToken ct)
        {
            return Task.FromResult(1L);
        }

        public Task<long> UpsertRetailerProductAsync(
            long retailerId,
            string externalSku,
            string title,
            string? brand,
            string url,
            CancellationToken ct)
        {
            throw new NotSupportedException("PriceSyncService should call PersistBestMatchAsync directly.");
        }

        public Task InsertPriceSnapshotAsync(long retailerProductId, decimal price, decimal? promoPrice, CancellationToken ct)
        {
            throw new NotSupportedException("PriceSyncService should call PersistBestMatchAsync directly.");
        }

        public Task UpsertIngredientProductMatchAsync(long foodNodeId, long retailerProductId, CancellationToken ct)
        {
            throw new NotSupportedException("PriceSyncService should call PersistBestMatchAsync directly.");
        }

        public Task PersistBestMatchAsync(
            long retailerId,
            IngredientToScrape ingredient,
            ScrapedProduct best,
            CancellationToken ct)
        {
            PersistedMatches.Add((retailerId, ingredient, best));
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<IngredientToScrape>> GetActiveIngredientsAsync(CancellationToken ct)
        {
            IReadOnlyList<IngredientToScrape> ingredients =
            [
                new(15, "milk"),
                new(16, "eggs"),
                new(17, "bread"),
                new(18, "butter")
            ];

            return Task.FromResult(ingredients);
        }
    }
}
