using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using FridgeChef.Pricing.Application;
using Microsoft.Extensions.Logging;

namespace FridgeChef.Pricing.Infrastructure.Scraping;

public sealed class HttpPyaterochkaScraper : IRetailerScraper
{
    private readonly ILogger<HttpPyaterochkaScraper> _logger;
    private readonly CookieContainer _cookieContainer;
    private readonly HttpClient _httpClient;
    private volatile bool _cookiesSet;

    private const string SearchUrlTemplate = "https://5ka.ru/search/?text={0}";
    private const int DelayBetweenRequestsMs = 1_500;

    public string RetailerCode => "pyaterochka";

    public HttpPyaterochkaScraper(ILogger<HttpPyaterochkaScraper> logger)
    {
        _logger = logger;
        _cookieContainer = new CookieContainer();

        var handler = new HttpClientHandler
        {
            CookieContainer = _cookieContainer,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 5
        };

        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        _httpClient.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");
        _httpClient.DefaultRequestHeaders.Add("Accept",
            "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
        _httpClient.DefaultRequestHeaders.Add("Accept-Language", "ru-RU,ru;q=0.9,en-US;q=0.8,en;q=0.7");
        _httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
    }

    public void SetCookies(string cookieString)
    {
        foreach (var part in cookieString.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = part.Trim();
            var eqIdx = trimmed.IndexOf('=');
            if (eqIdx <= 0) continue;

            var name = trimmed[..eqIdx];
            var value = trimmed[(eqIdx + 1)..];

            _cookieContainer.Add(new Cookie(name, value, "/", ".5ka.ru"));
        }

        _cookiesSet = true;
        _logger.LogInformation("Set {Count} cookies for 5ka.ru",
            cookieString.Split(';', StringSplitOptions.RemoveEmptyEntries).Length);
    }

    public bool HasCookies => _cookiesSet;

    public async Task<IReadOnlyList<ScrapedProduct>> SearchAsync(
        string query, CancellationToken ct = default)
    {
        if (!_cookiesSet)
        {
            _logger.LogWarning(
                "No cookies set for 5ka.ru scraper. " +
                "Sidecar not running. Use POST /admin/pricing/search-test to test.");
            return [];
        }

        try
        {
            var url = string.Format(SearchUrlTemplate, Uri.EscapeDataString(query));
            _logger.LogDebug("Fetching: {Query}", query);

            var response = await _httpClient.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("HTTP {Status} for '{Query}'",
                    (int)response.StatusCode, query);
                return [];
            }

            var html = await response.Content.ReadAsStringAsync(ct);

            if (html.Contains("servicepipe.ru") || html.Contains("captcha"))
            {
                _logger.LogWarning(
                    "WAF challenge detected — cookies expired or invalid. " +
                    "Cookies expired. Restart sidecar: POST /admin/pricing/reconnect");
                _cookiesSet = false;
                return [];
            }

            var products = ExtractProducts(html, query);

            await Task.Delay(DelayBetweenRequestsMs, ct);
            return products;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error fetching '{Query}'", query);
            return [];
        }
    }

    private List<ScrapedProduct> ExtractProducts(string html, string query)
    {

        var match = Regex.Match(html,
            @"<script\s+id=""__NEXT_DATA__""[^>]*>(.*?)</script>",
            RegexOptions.Singleline);

        if (!match.Success)
        {
            _logger.LogDebug("No __NEXT_DATA__ found for '{Query}'", query);
            return [];
        }

        var json = match.Groups[1].Value;
        return ParseNextData(json, query);
    }

    private List<ScrapedProduct> ParseNextData(string json, string query)
    {
        var results = new List<ScrapedProduct>();
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("props", out var props) &&
                props.TryGetProperty("pageProps", out var pp) &&
                pp.TryGetProperty("initialData", out var id) &&
                id.TryGetProperty("search", out var s) &&
                s.TryGetProperty("products", out var products))
            {
                foreach (var p in products.EnumerateArray())
                {
                    var scraped = ParseProduct(p);
                    if (scraped is not null) results.Add(scraped);
                }
                return results;
            }

            if (TryFindProductsArray(root, out var found, 0))
            {
                foreach (var p in found.EnumerateArray())
                {
                    var scraped = ParseProduct(p);
                    if (scraped is not null) results.Add(scraped);
                }
            }
            else
            {
                _logger.LogDebug("No products found in __NEXT_DATA__ for '{Query}'", query);
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse __NEXT_DATA__ for '{Query}'", query);
        }
        return results;
    }

    private static bool TryFindProductsArray(
        JsonElement el, out JsonElement products, int depth)
    {
        products = default;
        if (depth > 8 || el.ValueKind != JsonValueKind.Object) return false;

        foreach (var prop in el.EnumerateObject())
        {
            if (prop.Name == "products" &&
                prop.Value.ValueKind == JsonValueKind.Array &&
                prop.Value.GetArrayLength() > 0)
            {
                var first = prop.Value[0];
                if (first.TryGetProperty("id", out _) ||
                    first.TryGetProperty("plu", out _) ||
                    first.TryGetProperty("name", out _))
                {
                    products = prop.Value;
                    return true;
                }
            }

            if (prop.Value.ValueKind == JsonValueKind.Object &&
                TryFindProductsArray(prop.Value, out products, depth + 1))
                return true;
        }
        return false;
    }

    private static ScrapedProduct? ParseProduct(JsonElement p)
    {
        var id = GetStringOrNumber(p, "id") ?? GetStringOrNumber(p, "plu") ?? "";
        if (string.IsNullOrEmpty(id)) return null;

        var name = p.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
        if (string.IsNullOrEmpty(name)) return null;

        var brand = p.TryGetProperty("brand", out var b) ? b.GetString() : null;

        decimal regularPrice = 0;
        decimal? discountPrice = null;

        if (p.TryGetProperty("price", out var priceProp))
        {
            var current = Dec(priceProp);
            if (p.TryGetProperty("oldPrice", out var old) &&
                old.ValueKind != JsonValueKind.Null)
            {
                var oldVal = Dec(old);
                if (oldVal > current && oldVal > 0)
                { regularPrice = oldVal; discountPrice = current; }
                else regularPrice = current;
            }
            else regularPrice = current;
        }

        if (regularPrice == 0 && p.TryGetProperty("prices", out var prices))
        {
            if (prices.TryGetProperty("regular", out var rp)) regularPrice = Dec(rp);
            if (prices.TryGetProperty("discount", out var dp) &&
                dp.ValueKind != JsonValueKind.Null)
            {
                var d = Dec(dp);
                if (d > 0 && d < regularPrice) discountPrice = d;
            }
        }

        if (regularPrice <= 0) return null;

        var slug = p.TryGetProperty("slug", out var sl) ? sl.GetString() : null;
        var url = !string.IsNullOrEmpty(slug)
            ? $"https://5ka.ru/product/{slug}/"
            : $"https://5ka.ru/product/{id}";

        return new ScrapedProduct(id, name, brand, regularPrice, discountPrice, url);
    }

    private static string? GetStringOrNumber(JsonElement el, string prop)
    {
        if (!el.TryGetProperty(prop, out var val)) return null;
        return val.ValueKind switch
        {
            JsonValueKind.String => val.GetString(),
            JsonValueKind.Number => val.GetInt64().ToString(),
            _ => null
        };
    }

    private static decimal Dec(JsonElement el) => el.ValueKind switch
    {
        JsonValueKind.Number => el.GetDecimal(),
        JsonValueKind.String => decimal.TryParse(el.GetString(), out var d) ? d : 0,
        _ => 0
    };
}
