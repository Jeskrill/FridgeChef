using FridgeChef.Domain.Pantry;
using FridgeChef.Domain.UserPreferences;
using FridgeChef.Domain.Favorites;
using Microsoft.EntityFrameworkCore;

namespace FridgeChef.Infrastructure.Persistence.UserDomain;

internal sealed class PantryRepository : IPantryRepository
{
    private readonly FridgeChefDbContext _db;
    public PantryRepository(FridgeChefDbContext db) => _db = db;

    public async Task<IReadOnlyList<PantryItem>> GetByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        await _db.PantryItems.Where(p => p.UserId == userId).OrderBy(p => p.CreatedAt).ToListAsync(ct);

    public async Task<PantryItem?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _db.PantryItems.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<bool> ExistsAsync(Guid userId, long foodNodeId, CancellationToken ct = default) =>
        await _db.PantryItems.AnyAsync(p => p.UserId == userId && p.FoodNodeId == foodNodeId, ct);

    public async Task AddAsync(PantryItem item, CancellationToken ct = default)
    {
        _db.PantryItems.Add(item);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(PantryItem item, CancellationToken ct = default)
    {
        _db.PantryItems.Update(item);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await _db.PantryItems.Where(p => p.Id == id).ExecuteDeleteAsync(ct);
    }

    public async Task<IReadOnlySet<long>> GetFoodNodeIdsByUserAsync(Guid userId, CancellationToken ct = default)
    {
        var ids = await _db.PantryItems
            .Where(p => p.UserId == userId)
            .Select(p => p.FoodNodeId)
            .ToListAsync(ct);
        return ids.ToHashSet();
    }
}

internal sealed class UserPreferencesRepository : IUserPreferencesRepository
{
    private readonly FridgeChefDbContext _db;
    public UserPreferencesRepository(FridgeChefDbContext db) => _db = db;

    // Allergens
    public async Task<IReadOnlyList<UserAllergen>> GetAllergensAsync(Guid userId, CancellationToken ct = default) =>
        await _db.UserAllergens.Where(a => a.UserId == userId).ToListAsync(ct);

    public async Task AddAllergenAsync(UserAllergen allergen, CancellationToken ct = default)
    {
        _db.UserAllergens.Add(allergen);
        await _db.SaveChangesAsync(ct);
    }

    public async Task RemoveAllergenAsync(Guid userId, long foodNodeId, CancellationToken ct = default)
    {
        await _db.UserAllergens
            .Where(a => a.UserId == userId && a.FoodNodeId == foodNodeId)
            .ExecuteDeleteAsync(ct);
    }

    // Favorite foods
    public async Task<IReadOnlyList<UserFavoriteFood>> GetFavoriteFoodsAsync(Guid userId, CancellationToken ct = default) =>
        await _db.UserFavoriteFoods.Where(f => f.UserId == userId).ToListAsync(ct);

    public async Task AddFavoriteFoodAsync(UserFavoriteFood food, CancellationToken ct = default)
    {
        _db.UserFavoriteFoods.Add(food);
        await _db.SaveChangesAsync(ct);
    }

    public async Task RemoveFavoriteFoodAsync(Guid userId, long foodNodeId, CancellationToken ct = default)
    {
        await _db.UserFavoriteFoods
            .Where(f => f.UserId == userId && f.FoodNodeId == foodNodeId)
            .ExecuteDeleteAsync(ct);
    }

    // Excluded foods
    public async Task<IReadOnlyList<UserExcludedFood>> GetExcludedFoodsAsync(Guid userId, CancellationToken ct = default) =>
        await _db.UserExcludedFoods.Where(e => e.UserId == userId).ToListAsync(ct);

    public async Task AddExcludedFoodAsync(UserExcludedFood food, CancellationToken ct = default)
    {
        _db.UserExcludedFoods.Add(food);
        await _db.SaveChangesAsync(ct);
    }

    public async Task RemoveExcludedFoodAsync(Guid userId, long foodNodeId, CancellationToken ct = default)
    {
        await _db.UserExcludedFoods
            .Where(e => e.UserId == userId && e.FoodNodeId == foodNodeId)
            .ExecuteDeleteAsync(ct);
    }

    // Diets
    public async Task<IReadOnlyList<UserDefaultDiet>> GetDefaultDietsAsync(Guid userId, CancellationToken ct = default) =>
        await _db.UserDefaultDiets.Where(d => d.UserId == userId).ToListAsync(ct);

    public async Task ReplaceDefaultDietsAsync(Guid userId, IReadOnlyList<long> taxonIds, CancellationToken ct = default)
    {
        // Delete existing
        await _db.UserDefaultDiets.Where(d => d.UserId == userId).ExecuteDeleteAsync(ct);

        // Add new
        var newDiets = taxonIds.Select(id => new UserDefaultDiet
        {
            UserId = userId,
            TaxonId = id,
            CreatedAt = DateTime.UtcNow
        });
        _db.UserDefaultDiets.AddRange(newDiets);
        await _db.SaveChangesAsync(ct);
    }

    // For matching engine
    public async Task<IReadOnlySet<long>> GetAllergenFoodNodeIdsAsync(Guid userId, CancellationToken ct = default)
    {
        var ids = await _db.UserAllergens.Where(a => a.UserId == userId).Select(a => a.FoodNodeId).ToListAsync(ct);
        return ids.ToHashSet();
    }

    public async Task<IReadOnlySet<long>> GetFavoriteFoodNodeIdsAsync(Guid userId, CancellationToken ct = default)
    {
        var ids = await _db.UserFavoriteFoods.Where(f => f.UserId == userId).Select(f => f.FoodNodeId).ToListAsync(ct);
        return ids.ToHashSet();
    }

    public async Task<IReadOnlySet<long>> GetDefaultDietTaxonIdsAsync(Guid userId, CancellationToken ct = default)
    {
        var ids = await _db.UserDefaultDiets.Where(d => d.UserId == userId).Select(d => d.TaxonId).ToListAsync(ct);
        return ids.ToHashSet();
    }
}

internal sealed class FavoriteRecipeRepository : IFavoriteRecipeRepository
{
    private readonly FridgeChefDbContext _db;
    public FavoriteRecipeRepository(FridgeChefDbContext db) => _db = db;

    public async Task<IReadOnlyList<FavoriteRecipe>> GetByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        await _db.FavoriteRecipes.Where(f => f.UserId == userId).OrderByDescending(f => f.CreatedAt).ToListAsync(ct);

    public async Task<bool> ExistsAsync(Guid userId, Guid recipeId, CancellationToken ct = default) =>
        await _db.FavoriteRecipes.AnyAsync(f => f.UserId == userId && f.RecipeId == recipeId, ct);

    public async Task AddAsync(FavoriteRecipe favorite, CancellationToken ct = default)
    {
        _db.FavoriteRecipes.Add(favorite);
        await _db.SaveChangesAsync(ct);
    }

    public async Task RemoveAsync(Guid userId, Guid recipeId, CancellationToken ct = default)
    {
        await _db.FavoriteRecipes
            .Where(f => f.UserId == userId && f.RecipeId == recipeId)
            .ExecuteDeleteAsync(ct);
    }
}
