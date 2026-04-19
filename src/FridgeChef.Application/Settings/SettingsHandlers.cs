using FridgeChef.Domain.Common;
using FridgeChef.Domain.UserPreferences;

namespace FridgeChef.Application.Settings;

// ── Response DTOs ──
public sealed record AllergenResponse(long FoodNodeId, string Severity);
public sealed record FavoriteFoodResponse(long FoodNodeId);
public sealed record ExcludedFoodResponse(long FoodNodeId);
public sealed record UserDietResponse(long TaxonId);

// ── Request DTOs ──
public sealed record AddAllergenRequest(long FoodNodeId, string Severity = "strict");
public sealed record AddFavoriteFoodRequest(long FoodNodeId);
public sealed record AddExcludedFoodRequest(long FoodNodeId);
public sealed record UpdateDietsRequest(long[] TaxonIds);

// ── Allergens ──
public sealed class GetAllergensHandler(IUserPreferencesRepository prefs)
{
    public async Task<IReadOnlyList<AllergenResponse>> HandleAsync(Guid userId, CancellationToken ct = default)
    {
        var list = await prefs.GetAllergensAsync(userId, ct);
        return list.Select(a => new AllergenResponse(a.FoodNodeId, a.Severity.ToString())).ToList();
    }
}

public sealed class AddAllergenHandler(IUserPreferencesRepository prefs)
{
    public async Task<Result> HandleAsync(Guid userId, AddAllergenRequest request, CancellationToken ct = default)
    {
        var allergen = new UserAllergen
        {
            UserId = userId,
            FoodNodeId = request.FoodNodeId,
            Severity = Enum.TryParse<AllergenSeverity>(request.Severity, true, out var s) ? s : AllergenSeverity.Strict,
            CreatedAt = DateTime.UtcNow
        };
        await prefs.AddAllergenAsync(allergen, ct);
        return Result.Success();
    }
}

public sealed class RemoveAllergenHandler(IUserPreferencesRepository prefs)
{
    public async Task<Result> HandleAsync(Guid userId, long foodNodeId, CancellationToken ct = default)
    {
        await prefs.RemoveAllergenAsync(userId, foodNodeId, ct);
        return Result.Success();
    }
}

// ── Favorite Foods ──
public sealed class GetFavoriteFoodsHandler(IUserPreferencesRepository prefs)
{
    public async Task<IReadOnlyList<FavoriteFoodResponse>> HandleAsync(Guid userId, CancellationToken ct = default)
    {
        var list = await prefs.GetFavoriteFoodsAsync(userId, ct);
        return list.Select(f => new FavoriteFoodResponse(f.FoodNodeId)).ToList();
    }
}

public sealed class AddFavoriteFoodHandler(IUserPreferencesRepository prefs)
{
    public async Task<Result> HandleAsync(Guid userId, AddFavoriteFoodRequest request, CancellationToken ct = default)
    {
        await prefs.AddFavoriteFoodAsync(new UserFavoriteFood
        {
            UserId = userId, FoodNodeId = request.FoodNodeId, CreatedAt = DateTime.UtcNow
        }, ct);
        return Result.Success();
    }
}

public sealed class RemoveFavoriteFoodHandler(IUserPreferencesRepository prefs)
{
    public async Task<Result> HandleAsync(Guid userId, long foodNodeId, CancellationToken ct = default)
    {
        await prefs.RemoveFavoriteFoodAsync(userId, foodNodeId, ct);
        return Result.Success();
    }
}

// ── Excluded Foods ──
public sealed class GetExcludedFoodsHandler(IUserPreferencesRepository prefs)
{
    public async Task<IReadOnlyList<ExcludedFoodResponse>> HandleAsync(Guid userId, CancellationToken ct = default)
    {
        var list = await prefs.GetExcludedFoodsAsync(userId, ct);
        return list.Select(e => new ExcludedFoodResponse(e.FoodNodeId)).ToList();
    }
}

public sealed class AddExcludedFoodHandler(IUserPreferencesRepository prefs)
{
    public async Task<Result> HandleAsync(Guid userId, AddExcludedFoodRequest request, CancellationToken ct = default)
    {
        await prefs.AddExcludedFoodAsync(new UserExcludedFood
        {
            UserId = userId, FoodNodeId = request.FoodNodeId, CreatedAt = DateTime.UtcNow
        }, ct);
        return Result.Success();
    }
}

public sealed class RemoveExcludedFoodHandler(IUserPreferencesRepository prefs)
{
    public async Task<Result> HandleAsync(Guid userId, long foodNodeId, CancellationToken ct = default)
    {
        await prefs.RemoveExcludedFoodAsync(userId, foodNodeId, ct);
        return Result.Success();
    }
}

// ── Diets ──
public sealed class GetDietsHandler(IUserPreferencesRepository prefs)
{
    public async Task<IReadOnlyList<UserDietResponse>> HandleAsync(Guid userId, CancellationToken ct = default)
    {
        var list = await prefs.GetDefaultDietsAsync(userId, ct);
        return list.Select(d => new UserDietResponse(d.TaxonId)).ToList();
    }
}

public sealed class UpdateDietsHandler(IUserPreferencesRepository prefs)
{
    public async Task<Result> HandleAsync(Guid userId, UpdateDietsRequest request, CancellationToken ct = default)
    {
        await prefs.ReplaceDefaultDietsAsync(userId, request.TaxonIds, ct);
        return Result.Success();
    }
}
