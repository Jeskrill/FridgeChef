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
        await _db.PantryItems
            .Where(p => p.UserId == userId)
            .OrderBy(p => p.CreatedAt)
            .ThenBy(p => p.Id)
            .ToListAsync(ct);

    public async Task<PantryItem?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _db.PantryItems.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<bool> ExistsAsync(Guid userId, long foodNodeId, CancellationToken ct = default) =>
        await _db.PantryItems.AnyAsync(p => p.UserId == userId && p.FoodNodeId == foodNodeId, ct);

    public async Task<bool> TryAddAsync(PantryItem item, CancellationToken ct)
    {
        var insertedRows = await _db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO user_domain.pantry_items
                (id, user_id, food_node_id, quantity_value, unit_id, quantity_mode,
                 normalized_amount_g, normalized_amount_ml, source, note, expires_at, created_at, updated_at)
            VALUES
                ({item.Id}, {item.UserId}, {item.FoodNodeId}, {item.QuantityValue}, {item.UnitId},
                 {item.QuantityMode.ToString().ToLowerInvariant()}, {item.NormalizedAmountG},
                 {item.NormalizedAmountMl}, {item.Source}, {item.Note}, {item.ExpiresAt},
                 {item.CreatedAt}, {item.UpdatedAt})
            ON CONFLICT (user_id, food_node_id) DO NOTHING
            """, ct);

        return insertedRows == 1;
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
        await _db.UserAllergens
            .Where(a => a.UserId == userId)
            .OrderBy(a => a.CreatedAt)
            .ThenBy(a => a.FoodNodeId)
            .ToListAsync(ct);

    public async Task AddAllergenAsync(UserAllergen allergen, CancellationToken ct = default)
    {
        await _db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO user_domain.user_allergens (user_id, food_node_id, severity, created_at)
            VALUES ({allergen.UserId}, {allergen.FoodNodeId}, {allergen.Severity.ToString().ToLowerInvariant()}, {allergen.CreatedAt})
            ON CONFLICT (user_id, food_node_id) DO NOTHING
            """, ct);
    }

    public async Task RemoveAllergenAsync(Guid userId, long foodNodeId, CancellationToken ct = default)
    {
        await _db.UserAllergens
            .Where(a => a.UserId == userId && a.FoodNodeId == foodNodeId)
            .ExecuteDeleteAsync(ct);
    }

    // Favorite foods
    public async Task<IReadOnlyList<UserFavoriteFood>> GetFavoriteFoodsAsync(Guid userId, CancellationToken ct = default) =>
        await _db.UserFavoriteFoods
            .Where(f => f.UserId == userId)
            .OrderBy(f => f.CreatedAt)
            .ThenBy(f => f.FoodNodeId)
            .ToListAsync(ct);

    public async Task AddFavoriteFoodAsync(UserFavoriteFood food, CancellationToken ct = default)
    {
        await _db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO user_domain.user_favorite_foods (user_id, food_node_id, weight, created_at)
            VALUES ({food.UserId}, {food.FoodNodeId}, {food.Weight}, {food.CreatedAt})
            ON CONFLICT (user_id, food_node_id) DO NOTHING
            """, ct);
    }

    public async Task RemoveFavoriteFoodAsync(Guid userId, long foodNodeId, CancellationToken ct = default)
    {
        await _db.UserFavoriteFoods
            .Where(f => f.UserId == userId && f.FoodNodeId == foodNodeId)
            .ExecuteDeleteAsync(ct);
    }

    // Excluded foods
    public async Task<IReadOnlyList<UserExcludedFood>> GetExcludedFoodsAsync(Guid userId, CancellationToken ct = default) =>
        await _db.UserExcludedFoods
            .Where(e => e.UserId == userId)
            .OrderBy(e => e.CreatedAt)
            .ThenBy(e => e.FoodNodeId)
            .ToListAsync(ct);

    public async Task AddExcludedFoodAsync(UserExcludedFood food, CancellationToken ct = default)
    {
        await _db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO user_domain.user_excluded_foods (user_id, food_node_id, created_at)
            VALUES ({food.UserId}, {food.FoodNodeId}, {food.CreatedAt})
            ON CONFLICT (user_id, food_node_id) DO NOTHING
            """, ct);
    }

    public async Task RemoveExcludedFoodAsync(Guid userId, long foodNodeId, CancellationToken ct = default)
    {
        await _db.UserExcludedFoods
            .Where(e => e.UserId == userId && e.FoodNodeId == foodNodeId)
            .ExecuteDeleteAsync(ct);
    }

    // Diets
    public async Task<IReadOnlyList<UserDefaultDiet>> GetDefaultDietsAsync(Guid userId, CancellationToken ct = default) =>
        await _db.UserDefaultDiets
            .Where(d => d.UserId == userId)
            .OrderBy(d => d.TaxonId)
            .ToListAsync(ct);

    public async Task ReplaceDefaultDietsAsync(Guid userId, IReadOnlyList<long> taxonIds, CancellationToken ct = default)
    {
        var distinctTaxonIds = taxonIds.Distinct().ToArray();
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);

        await _db.UserDefaultDiets.Where(d => d.UserId == userId).ExecuteDeleteAsync(ct);

        if (distinctTaxonIds.Length > 0)
        {
            var createdAt = DateTime.UtcNow;
            var newDiets = distinctTaxonIds.Select(id => new UserDefaultDiet
            {
                UserId = userId,
                TaxonId = id,
                CreatedAt = createdAt
            });
            _db.UserDefaultDiets.AddRange(newDiets);
            await _db.SaveChangesAsync(ct);
        }

        await transaction.CommitAsync(ct);
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
        await _db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO user_domain.favorite_recipes (user_id, recipe_id, created_at)
            VALUES ({favorite.UserId}, {favorite.RecipeId}, {favorite.CreatedAt})
            ON CONFLICT (user_id, recipe_id) DO NOTHING
            """, ct);
    }

    public async Task RemoveAsync(Guid userId, Guid recipeId, CancellationToken ct = default)
    {
        await _db.FavoriteRecipes
            .Where(f => f.UserId == userId && f.RecipeId == recipeId)
            .ExecuteDeleteAsync(ct);
    }
}
