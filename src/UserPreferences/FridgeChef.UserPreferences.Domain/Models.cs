namespace FridgeChef.UserPreferences.Domain;

public enum AllergenSeverity
{
    Strict = 0,
    Mild = 1
}

public sealed record UserAllergen(
    Guid UserId,
    long FoodNodeId,
    AllergenSeverity Severity,
    DateTime CreatedAt);

public sealed record UserExcludedFood(
    Guid UserId,
    long FoodNodeId,
    DateTime CreatedAt);

public sealed record UserFavoriteFood(
    Guid UserId,
    long FoodNodeId,
    decimal Weight,
    DateTime CreatedAt);

public sealed record UserDefaultDiet(
    Guid UserId,
    long TaxonId,
    DateTime CreatedAt);

// Кухонные предпочтения пользователя — рецепты из этих кухонь получают бонус к рейтингу.
public sealed record UserPreferredCuisine(
    Guid UserId,
    long TaxonId,
    DateTime CreatedAt);

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

    // Кухонные предпочтения
    Task<IReadOnlyList<UserPreferredCuisine>> GetPreferredCuisinesAsync(Guid userId, CancellationToken ct = default);
    Task ReplacePreferredCuisinesAsync(Guid userId, IReadOnlyList<long> taxonIds, CancellationToken ct = default);
    Task<IReadOnlySet<long>> GetPreferredCuisineTaxonIdsAsync(Guid userId, CancellationToken ct = default);

    Task<IReadOnlySet<long>> GetAllergenFoodNodeIdsAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlySet<long>> GetFavoriteFoodNodeIdsAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlySet<long>> GetDefaultDietTaxonIdsAsync(Guid userId, CancellationToken ct = default);
}
