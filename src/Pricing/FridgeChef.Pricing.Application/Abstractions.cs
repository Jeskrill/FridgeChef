namespace FridgeChef.Pricing.Application;

/// <summary>
/// Product scraped from a retailer search page.
/// </summary>
public sealed record ScrapedProduct(
    string ExternalSku,
    string Title,
    string? Brand,
    decimal RegularPrice,
    decimal? DiscountPrice,
    string ProductUrl);

/// <summary>
/// Scrapes product search results from an online retailer.
/// </summary>
public interface IRetailerScraper
{
    /// <summary>Retailer code (e.g. "pyaterochka").</summary>
    string RetailerCode { get; }

    /// <summary>
    /// Searches the retailer for the given ingredient query.
    /// Returns the first page of results ordered by relevance.
    /// </summary>
    Task<IReadOnlyList<ScrapedProduct>> SearchAsync(string query, CancellationToken ct = default);
}

/// <summary>
/// Extended scraper interface that supports batch queries.
/// Implemented by scrapers that maintain a persistent session (e.g. Puppeteer sidecar).
/// </summary>
public interface IBatchRetailerScraper : IRetailerScraper
{
    /// <summary>
    /// Searches for multiple queries in one call.
    /// Returns a dictionary keyed by original query string.
    /// </summary>
    Task<Dictionary<string, IReadOnlyList<ScrapedProduct>>> SearchBatchAsync(
        IReadOnlyList<string> queries, CancellationToken ct = default);
}

/// <summary>
/// Persists scraped price data into the pricing schema.
/// </summary>
public interface IPriceSyncRepository
{
    Task<long> EnsureRetailerAsync(string code, string name, string baseUrl, CancellationToken ct = default);

    Task<long> UpsertRetailerProductAsync(
        long retailerId, string externalSku, string title, string? brand,
        string url, CancellationToken ct = default);

    Task InsertPriceSnapshotAsync(
        long retailerProductId, decimal price, decimal? promoPrice,
        CancellationToken ct = default);

    Task UpsertIngredientProductMatchAsync(
        long foodNodeId, long retailerProductId, CancellationToken ct = default);

    Task PersistBestMatchAsync(
        long retailerId,
        IngredientToScrape ingredient,
        ScrapedProduct best,
        CancellationToken ct);

    Task<IReadOnlyList<IngredientToScrape>> GetActiveIngredientsAsync(CancellationToken ct = default);
}

/// <summary>
/// An ingredient that needs a price lookup.
/// </summary>
public sealed record IngredientToScrape(long FoodNodeId, string CanonicalName);
