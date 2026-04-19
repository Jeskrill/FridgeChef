namespace FridgeChef.Domain.Pricing;

/// <summary>Mapped to pricing.retailers.</summary>
public sealed class Retailer
{
    public long Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string BaseUrl { get; set; } = null!;
    public bool IsActive { get; set; } = true;
}

/// <summary>Mapped to pricing.retailer_products.</summary>
public sealed class RetailerProduct
{
    public long Id { get; set; }
    public long RetailerId { get; set; }
    public string ExternalSku { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Url { get; set; } = null!;
    public string? Brand { get; set; }
    public decimal? PackageQuantityValue { get; set; }
    public long? PackageUnitId { get; set; }
    public decimal? PackageWeightG { get; set; }
    public bool IsActive { get; set; } = true;

    public Retailer Retailer { get; set; } = null!;
}

/// <summary>Mapped to pricing.ingredient_product_matches.</summary>
public sealed class IngredientProductMatch
{
    public long Id { get; set; }
    public long FoodNodeId { get; set; }
    public long RetailerProductId { get; set; }
    public string MatchType { get; set; } = null!;
    public decimal Score { get; set; }
    public bool IsPrimary { get; set; }

    public RetailerProduct RetailerProduct { get; set; } = null!;
}

/// <summary>Mapped to pricing.price_snapshots.</summary>
public sealed class PriceSnapshot
{
    public long Id { get; set; }
    public long RetailerProductId { get; set; }
    public DateTime CapturedAt { get; set; }
    public decimal Price { get; set; }
    public decimal? PromoPrice { get; set; }
    public string Currency { get; set; } = "RUB";
    public decimal? PricePerKg { get; set; }
    public decimal? PricePerL { get; set; }
    public bool? InStock { get; set; }

    public RetailerProduct RetailerProduct { get; set; } = null!;
}

public interface IPricingRepository
{
    Task<IReadOnlyList<IngredientPrice>> GetPricesForFoodNodesAsync(
        IEnumerable<long> foodNodeIds,
        CancellationToken ct = default);
}

public sealed record IngredientPrice(
    long FoodNodeId,
    string ProductTitle,
    decimal Price,
    decimal? PromoPrice,
    string? ProductUrl,
    string RetailerName);
