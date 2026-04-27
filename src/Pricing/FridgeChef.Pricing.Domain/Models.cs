namespace FridgeChef.Pricing.Domain;

public sealed record IngredientPrice(
    long FoodNodeId,
    string ProductTitle,
    decimal Price,
    decimal? PromoPrice,
    string? ProductUrl,
    string RetailerName);

public sealed record ScrapedProduct(
    string ExternalSku,
    string Title,
    string? Brand,
    decimal RegularPrice,
    decimal? DiscountPrice,
    string ProductUrl);
