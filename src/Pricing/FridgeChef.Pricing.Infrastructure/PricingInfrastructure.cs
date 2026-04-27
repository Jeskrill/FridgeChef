using System.Net;
using FridgeChef.Pricing.Application;
using FridgeChef.Pricing.Domain;
using FridgeChef.Pricing.Infrastructure.BackgroundJobs;
using FridgeChef.Pricing.Infrastructure.Persistence;
using FridgeChef.Pricing.Infrastructure.Scraping;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FridgeChef.Pricing.Infrastructure;

internal sealed class PricingDbContext : DbContext
{
    public PricingDbContext(DbContextOptions<PricingDbContext> options) : base(options)
    {
        ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTrackingWithIdentityResolution;
    }
}

internal sealed class PricingRepository : IPricingRepository
{
    private readonly PricingDbContext _db;
    public PricingRepository(PricingDbContext db) => _db = db;

    public async Task<IReadOnlyList<IngredientPrice>> GetPricesForFoodNodesAsync(
        IEnumerable<long> foodNodeIds, CancellationToken ct)
    {
        var ids = foodNodeIds.ToList();
        if (ids.Count == 0) return Array.Empty<IngredientPrice>();

        var result = await _db.Database
            .SqlQueryRaw<IngredientPrice>(
                """
                SELECT DISTINCT ON (ipm.food_node_id)
                    ipm.food_node_id AS "FoodNodeId",
                    rp.title AS "ProductTitle",
                    ps.price AS "Price",
                    ps.promo_price AS "PromoPrice",
                    rp.url AS "ProductUrl",
                    r.name AS "RetailerName"
                FROM pricing.ingredient_product_matches ipm
                JOIN pricing.retailer_products rp ON rp.id = ipm.retailer_product_id
                JOIN pricing.retailers r ON r.id = rp.retailer_id
                JOIN LATERAL (
                    SELECT price, promo_price
                    FROM pricing.price_snapshots
                    WHERE retailer_product_id = rp.id
                    ORDER BY captured_at DESC
                    LIMIT 1
                ) ps ON true
                WHERE ipm.food_node_id = ANY({0})
                ORDER BY ipm.food_node_id, ipm.score DESC
                """,
                ids.ToArray())
            .ToListAsync(ct);

        return result;
    }
}

public static class PricingInfrastructureExtensions
{
    public static IServiceCollection AddPricingInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<PricingDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IPricingRepository, PricingRepository>();

        services.AddScoped<IPriceSyncRepository, PriceSyncRepository>();
        services.AddScoped<PriceSyncService>();
        services.AddSingleton<PriceSyncRunner>();
        services.Configure<PriceSyncOptions>(
            configuration.GetSection(PriceSyncOptions.Section));

        services.Configure<PuppeteerScraperOptions>(
            configuration.GetSection(PuppeteerScraperOptions.Section));

        var scraperOptions = configuration
            .GetSection(PuppeteerScraperOptions.Section)
            .Get<PuppeteerScraperOptions>() ?? new PuppeteerScraperOptions();

        services.AddHttpClient<PuppeteerPyaterochkaScraper>(client =>
        {
            client.BaseAddress = new Uri(scraperOptions.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(scraperOptions.SingleTimeoutSeconds);
        });
        services.AddSingleton<IRetailerScraper>(sp =>
            sp.GetRequiredService<PuppeteerPyaterochkaScraper>());

        services.AddHttpClient<HttpPyaterochkaScraper>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");
            client.DefaultRequestHeaders.Add("Accept",
                "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            client.DefaultRequestHeaders.Add("Accept-Language", "ru-RU,ru;q=0.9,en-US;q=0.8,en;q=0.7");
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 5
        });

        services.AddHostedService<PriceSyncBackgroundService>();

        return services;
    }
}
