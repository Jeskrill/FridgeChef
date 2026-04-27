using FridgeChef.Favorites.Application.UseCases;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FridgeChef.Favorites.Infrastructure;

internal sealed class FavoriteRecipeEntity
{
    public Guid UserId { get; set; }
    public Guid RecipeId { get; set; }
    public DateTime CreatedAt { get; set; }
}

internal sealed class FavoritesDbContext : DbContext
{
    public FavoritesDbContext(DbContextOptions<FavoritesDbContext> options) : base(options)
    {
        ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTrackingWithIdentityResolution;
    }

    internal DbSet<FavoriteRecipeEntity> FavoriteRecipes => Set<FavoriteRecipeEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var builder = modelBuilder.Entity<FavoriteRecipeEntity>();
        builder.ToTable("favorite_recipes", "user_domain");
        builder.HasKey(f => new { f.UserId, f.RecipeId });
        builder.Property(f => f.UserId).HasColumnName("user_id");
        builder.Property(f => f.RecipeId).HasColumnName("recipe_id");
        builder.Property(f => f.CreatedAt).HasColumnName("created_at");
    }
}

internal sealed class FavoriteRecipeRepository : IFavoriteRecipeRepository
{
    private readonly FavoritesDbContext _db;
    public FavoriteRecipeRepository(FavoritesDbContext db) => _db = db;

    public async Task<IReadOnlyList<FavoriteRecipeResponse>> GetByUserIdAsync(
        Guid userId, IRecipeSummaryProvider recipes, CancellationToken ct)
    {
        var entities = await _db.FavoriteRecipes
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync(ct);

        if (entities.Count == 0) return Array.Empty<FavoriteRecipeResponse>();

        var summaries = await recipes.GetByIdsAsync(entities.Select(f => f.RecipeId), ct);
        var byId = summaries.ToDictionary(r => r.Id);

        return entities
            .Where(f => byId.ContainsKey(f.RecipeId))
            .Select(f => new FavoriteRecipeResponse(
                f.RecipeId, byId[f.RecipeId].Slug,
                byId[f.RecipeId].Title, byId[f.RecipeId].ImageUrl, f.CreatedAt))
            .ToList();
    }

    public async Task<bool> ExistsAsync(Guid userId, Guid recipeId, CancellationToken ct) =>
        await _db.FavoriteRecipes.AnyAsync(f => f.UserId == userId && f.RecipeId == recipeId, ct);

    public async Task AddAsync(Guid userId, Guid recipeId, CancellationToken ct)
    {
        _db.FavoriteRecipes.Add(new FavoriteRecipeEntity
        {
            UserId = userId,
            RecipeId = recipeId,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);
    }

    public Task<int> CountTotalAsync(CancellationToken ct) =>
        _db.FavoriteRecipes.CountAsync(ct);

    public async Task<IReadOnlyList<(Guid RecipeId, int Count)>> GetMostFavoritedAsync(
        int limit, CancellationToken ct)
    {
        var result = await _db.FavoriteRecipes
            .GroupBy(f => f.RecipeId)
            .Select(g => new { RecipeId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(limit)
            .ToListAsync(ct);
        return result.Select(x => (x.RecipeId, x.Count)).ToList();
    }

    public async Task RemoveAsync(Guid userId, Guid recipeId, CancellationToken ct) =>
        await _db.FavoriteRecipes
            .Where(f => f.UserId == userId && f.RecipeId == recipeId)
            .ExecuteDeleteAsync(ct);
}

public static class FavoritesInfrastructureExtensions
{
    public static IServiceCollection AddFavoritesInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<FavoritesDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IFavoriteRecipeRepository, FavoriteRecipeRepository>();

        return services;
    }
}
