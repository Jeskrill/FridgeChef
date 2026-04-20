namespace FridgeChef.Pricing.Application;

// Товар, найденный при поиске на сайте ретейлера.
public sealed record ScrapedProduct(
    string ExternalSku,
    string Title,
    string? Brand,
    decimal RegularPrice,
    decimal? DiscountPrice,
    string ProductUrl);

// Интерфейс скрапера — выполняет поиск товаров в онлайн-ретейлере.
public interface IRetailerScraper
{
    // Код ретейлера, например "pyaterochka".
    string RetailerCode { get; }

    // Ищет товар в ретейлере по названию ингредиента.
    // Возвращает первую страницу результатов в порядке релевантности.
    Task<IReadOnlyList<ScrapedProduct>> SearchAsync(string query, CancellationToken ct = default);
}

// Расширенный скрапер с поддержкой пакетных запросов.
// Реализуется скраперами с постоянной сессией (например Puppeteer sidecar).
public interface IBatchRetailerScraper : IRetailerScraper
{
    // Ищет несколько запросов за один вызов.
    // Возвращает словарь с ключами по оригинальной строке запроса.
    Task<Dictionary<string, IReadOnlyList<ScrapedProduct>>> SearchBatchAsync(
        IReadOnlyList<string> queries, CancellationToken ct = default);
}

// Сохраняет данные о ценах из скрапера в схему базы данных.
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

// Ингредиент, для которого требуется получить цену.
public sealed record IngredientToScrape(long FoodNodeId, string CanonicalName);
