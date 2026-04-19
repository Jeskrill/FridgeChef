using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FridgeChef.Pricing.Application;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FridgeChef.Pricing.Infrastructure.Scraping;

/// <summary>
/// Configuration for the Puppeteer sidecar scraper.
/// </summary>
public sealed class PuppeteerScraperOptions
{
    public const string Section = "Pricing:PuppeteerScraper";

    /// <summary>Base URL of the Node.js Puppeteer sidecar server.</summary>
    public string BaseUrl { get; set; } = "http://localhost:3333";

    /// <summary>Max queries per batch request to the sidecar.</summary>
    public int BatchSize { get; set; } = 20;

    /// <summary>HTTP timeout for a single search request (seconds).</summary>
    public int SingleTimeoutSeconds { get; set; } = 45;

    /// <summary>HTTP timeout for a batch request (seconds).</summary>
    public int BatchTimeoutSeconds { get; set; } = 600;
}

/// <summary>
/// Scrapes 5ka.ru via a Puppeteer+Stealth Node.js sidecar server.
///
/// Architecture:
///   .NET ─HTTP→ Node.js (localhost:3333) ─Puppeteer→ 5ka.ru
///
/// The Node.js server maintains a warm Chromium browser session
/// with stealth plugin that auto-solves ServicePipe WAF challenges.
/// No manual cookie management needed.
/// </summary>
public sealed class PuppeteerPyaterochkaScraper : IBatchRetailerScraper, IDisposable
{
    private readonly HttpClient _http;
    private readonly PuppeteerScraperOptions _options;
    private readonly ILogger<PuppeteerPyaterochkaScraper> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public string RetailerCode => "pyaterochka";

    public PuppeteerPyaterochkaScraper(
        IOptions<PuppeteerScraperOptions> options,
        ILogger<PuppeteerPyaterochkaScraper> logger)
    {
        _options = options.Value;
        _logger = logger;
        _http = new HttpClient
        {
            BaseAddress = new Uri(_options.BaseUrl),
            Timeout = TimeSpan.FromSeconds(_options.SingleTimeoutSeconds),
        };
    }

    /// <summary>
    /// Searches for a single ingredient query via the Puppeteer sidecar.
    /// </summary>
    public async Task<IReadOnlyList<ScrapedProduct>> SearchAsync(
        string query, CancellationToken ct = default)
    {
        try
        {
            var url = $"/search?q={Uri.EscapeDataString(query)}";
            var response = await _http.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Puppeteer sidecar returned HTTP {Status} for '{Query}'",
                    (int)response.StatusCode, query);
                return [];
            }

            var result = await response.Content
                .ReadFromJsonAsync<ScrapeResult>(JsonOpts, ct);

            if (result is null) return [];

            if (result.Error is not null)
            {
                _logger.LogWarning(
                    "Scraper error for '{Query}': {Error} (source: {Source})",
                    query, result.Error, result.Source);
                return [];
            }

            _logger.LogDebug(
                "Found {Count} products for '{Query}' (source: {Source})",
                result.Products?.Count ?? 0, query, result.Source);

            return result.Products?
                .Select(MapProduct)
                .Where(p => p is not null)
                .Cast<ScrapedProduct>()
                .ToList()
                ?? [];
        }
        catch (TaskCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error calling Puppeteer sidecar for '{Query}'", query);
            return [];
        }
    }

    /// <summary>
    /// Sends a batch of queries to the Puppeteer sidecar.
    /// Returns a dictionary: query → products.
    /// </summary>
    public async Task<Dictionary<string, IReadOnlyList<ScrapedProduct>>> SearchBatchAsync(
        IReadOnlyList<string> queries, CancellationToken ct = default)
    {
        var results = new Dictionary<string, IReadOnlyList<ScrapedProduct>>();
        if (queries.Count == 0) return results;

        try
        {
            using var batchClient = new HttpClient
            {
                BaseAddress = new Uri(_options.BaseUrl),
                Timeout = TimeSpan.FromSeconds(_options.BatchTimeoutSeconds),
            };

            var payload = JsonSerializer.Serialize(
                new { queries },
                JsonOpts);

            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var response = await batchClient.PostAsync("/search/batch", content, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Batch request failed with HTTP {Status}",
                    (int)response.StatusCode);
                return results;
            }

            var batch = await response.Content
                .ReadFromJsonAsync<BatchResult>(JsonOpts, ct);

            if (batch?.Results is null) return results;

            foreach (var item in batch.Results)
            {
                if (item.Query is null) continue;
                var products = item.Products?
                    .Select(MapProduct)
                    .Where(p => p is not null)
                    .Cast<ScrapedProduct>()
                    .ToList() ?? [];
                results[item.Query] = products;
            }

            _logger.LogInformation(
                "Batch search: {Completed}/{Total} queries completed, {Products} total products",
                batch.Completed, batch.Total,
                results.Values.Sum(v => v.Count));
        }
        catch (TaskCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Batch search failed");
        }

        return results;
    }

    /// <summary>
    /// Checks if the Puppeteer sidecar is running and the browser is ready.
    /// </summary>
    public async Task<(bool ready, string? error)> CheckHealthAsync(
        CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetAsync("/health", ct);
            if (!response.IsSuccessStatusCode)
                return (false, $"HTTP {(int)response.StatusCode}");

            var health = await response.Content
                .ReadFromJsonAsync<HealthResult>(JsonOpts, ct);

            return (health?.Status == "ready", health?.Error);
        }
        catch (Exception ex)
        {
            return (false, $"Connection failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Requests the sidecar to restart its browser instance.
    /// </summary>
    public async Task RestartBrowserAsync(CancellationToken ct = default)
    {
        try
        {
            await _http.PostAsync("/restart", null, ct);
            _logger.LogInformation("Browser restart requested");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to request browser restart");
        }
    }

    /// <summary>
    /// Sets cookies on the Puppeteer sidecar browser from a real browser session.
    /// Format: "name1=value1; name2=value2; ..."
    /// </summary>
    public async Task<bool> SetCookiesAsync(string cookieString, CancellationToken ct = default)
    {
        try
        {
            var payload = JsonSerializer.Serialize(
                new { cookies = cookieString }, JsonOpts);
            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var response = await _http.PostAsync("/cookies", content, ct);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Cookies forwarded to Puppeteer sidecar");
                return true;
            }

            _logger.LogWarning("Failed to set cookies: HTTP {Status}",
                (int)response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error setting cookies on sidecar");
            return false;
        }
    }

    private static ScrapedProduct? MapProduct(ScrapeProductDto? p)
    {
        if (p is null) return null;
        if (string.IsNullOrEmpty(p.ExternalSku) || string.IsNullOrEmpty(p.Title))
            return null;
        if (p.RegularPrice <= 0) return null;

        return new ScrapedProduct(
            p.ExternalSku,
            p.Title,
            p.Brand,
            p.RegularPrice,
            p.DiscountPrice,
            p.ProductUrl ?? $"https://5ka.ru/product/{p.ExternalSku}");
    }

    public void Dispose() => _http.Dispose();

    // ─── DTOs for JSON deserialization ──────────────────────────────────

    private sealed record ScrapeResult(
        string? Query,
        List<ScrapeProductDto>? Products,
        string? Error,
        string? Source);

    private sealed record ScrapeProductDto(
        string ExternalSku,
        string Title,
        string? Brand,
        decimal RegularPrice,
        decimal? DiscountPrice,
        string? ProductUrl);

    private sealed record BatchResult(
        List<ScrapeResult>? Results,
        int Completed,
        int Total);

    private sealed record HealthResult(
        string? Status,
        string? Error);
}
