using FluentAssertions;
using FridgeChef.Pricing.Application;
using FridgeChef.Pricing.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FridgeChef.Backend.Tests.Pricing;

public sealed class PriceSyncRunnerTests
{
    [Fact]
    public async Task TryRunAsync_ShouldRejectConcurrentRun()
    {
        var scraper = new FakeScraper();
        var repository = new BlockingPriceSyncRepository();
        var service = new PriceSyncService(
            scraper,
            repository,
            Options.Create(new PriceSyncOptions
            {
                BatchSize = 1,
                MaxIngredientsPerRun = 1
            }),
            NullLogger<PriceSyncService>.Instance);

        var runner = new PriceSyncRunner(
            new SingleServiceScopeFactory(service),
            NullLogger<PriceSyncRunner>.Instance);

        var firstRun = runner.TryRunAsync(CancellationToken.None);
        await repository.PersistStarted.Task;

        var secondRun = await runner.TryRunAsync(CancellationToken.None);

        secondRun.Should().BeFalse();
        repository.AllowPersist.SetResult();

        var firstRunResult = await firstRun;
        firstRunResult.Should().BeTrue();
    }

    private sealed class FakeScraper : IRetailerScraper
    {
        public string RetailerCode => "pyaterochka";

        public Task<IReadOnlyList<ScrapedProductDto>> SearchAsync(string query, CancellationToken ct)
        {
            IReadOnlyList<ScrapedProductDto> products =
            [
                new ScrapedProductDto("sku-1", "milk", "brand", 100, null, "https://example.test/milk")
            ];

            return Task.FromResult(products);
        }
    }

    private sealed class BlockingPriceSyncRepository : IPriceSyncRepository
    {
        public TaskCompletionSource PersistStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource AllowPersist { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

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
            throw new NotSupportedException();
        }

        public Task InsertPriceSnapshotAsync(long retailerProductId, decimal price, decimal? promoPrice, CancellationToken ct)
        {
            throw new NotSupportedException();
        }

        public Task UpsertIngredientProductMatchAsync(long foodNodeId, long retailerProductId, CancellationToken ct)
        {
            throw new NotSupportedException();
        }

        public async Task PersistBestMatchAsync(
            long retailerId,
            IngredientToScrape ingredient,
            ScrapedProductDto best,
            CancellationToken ct)
        {
            PersistStarted.TrySetResult();
            await AllowPersist.Task.WaitAsync(ct);
        }

        public Task<IReadOnlyList<IngredientToScrape>> GetActiveIngredientsAsync(CancellationToken ct)
        {
            IReadOnlyList<IngredientToScrape> ingredients =
            [
                new(15, "milk")
            ];

            return Task.FromResult(ingredients);
        }

        public Task<PricingStatsResponse> GetStatsAsync(CancellationToken ct) =>
            Task.FromResult(new PricingStatsResponse(0, 0, null));
    }

    private sealed class SingleServiceScopeFactory : IServiceScopeFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public SingleServiceScopeFactory(PriceSyncService service)
        {
            _serviceProvider = new SingleServiceProvider(service);
        }

        public IServiceScope CreateScope()
        {
            return new SingleServiceScope(_serviceProvider);
        }

        private sealed class SingleServiceScope : IServiceScope, IAsyncDisposable
        {
            public SingleServiceScope(IServiceProvider serviceProvider)
            {
                ServiceProvider = serviceProvider;
            }

            public IServiceProvider ServiceProvider { get; }

            public void Dispose()
            {
            }

            public ValueTask DisposeAsync()
            {
                return ValueTask.CompletedTask;
            }
        }

        private sealed class SingleServiceProvider : IServiceProvider
        {
            private readonly PriceSyncService _service;

            public SingleServiceProvider(PriceSyncService service)
            {
                _service = service;
            }

            public object? GetService(Type serviceType)
            {
                return serviceType == typeof(PriceSyncService) ? _service : null;
            }
        }
    }
}
