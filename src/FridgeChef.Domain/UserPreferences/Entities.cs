namespace FridgeChef.Domain.UserPreferences;

public enum AllergenSeverity
{
    Strict = 0,
    Mild = 1
}

/// <summary>Mapped to user_domain.user_allergens.</summary>
public sealed class UserAllergen
{
    public Guid UserId { get; set; }
    public long FoodNodeId { get; set; }
    public AllergenSeverity Severity { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>Mapped to user_domain.user_excluded_foods.</summary>
public sealed class UserExcludedFood
{
    public Guid UserId { get; set; }
    public long FoodNodeId { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>Mapped to user_domain.user_favorite_foods.</summary>
public sealed class UserFavoriteFood
{
    public Guid UserId { get; set; }
    public long FoodNodeId { get; set; }
    public decimal Weight { get; set; } = 1.0m;
    public DateTime CreatedAt { get; set; }
}

/// <summary>Mapped to user_domain.user_default_diets.</summary>
public sealed class UserDefaultDiet
{
    public Guid UserId { get; set; }
    public long TaxonId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public interface IUserPreferencesRepository
{
    Task<IReadOnlyList<UserAllergen>> GetAllergensAsync(Guid userId, CancellationToken ct = default);
    Task AddAllergenAsync(UserAllergen allergen, CancellationToken ct = default);
    Task RemoveAllergenAsync(Guid userId, long foodNodeId, CancellationToken ct = default);

    Task<IReadOnlyList<UserFavoriteFood>> GetFavoriteFoodsAsync(Guid userId, CancellationToken ct = default);
    Task AddFavoriteFoodAsync(UserFavoriteFood food, CancellationToken ct = default);
    Task RemoveFavoriteFoodAsync(Guid userId, long foodNodeId, CancellationToken ct = default);

    Task<IReadOnlyList<UserExcludedFood>> GetExcludedFoodsAsync(Guid userId, CancellationToken ct = default);
    Task AddExcludedFoodAsync(UserExcludedFood food, CancellationToken ct = default);
    Task RemoveExcludedFoodAsync(Guid userId, long foodNodeId, CancellationToken ct = default);

    Task<IReadOnlyList<UserDefaultDiet>> GetDefaultDietsAsync(Guid userId, CancellationToken ct = default);
    Task ReplaceDefaultDietsAsync(Guid userId, IReadOnlyList<long> taxonIds, CancellationToken ct = default);

    Task<IReadOnlySet<long>> GetAllergenFoodNodeIdsAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlySet<long>> GetFavoriteFoodNodeIdsAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlySet<long>> GetDefaultDietTaxonIdsAsync(Guid userId, CancellationToken ct = default);
}
