using FridgeChef.Favorites.Domain;
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

    public async Task<IReadOnlyList<FavoriteRecipe>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        var entities = await _db.FavoriteRecipes
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync(ct);
        return entities.Select(e => new FavoriteRecipe(e.UserId, e.RecipeId, e.CreatedAt)).ToList();
    }

    public async Task<bool> ExistsAsync(Guid userId, Guid recipeId, CancellationToken ct = default) =>
        await _db.FavoriteRecipes.AnyAsync(f => f.UserId == userId && f.RecipeId == recipeId, ct);

    public async Task AddAsync(FavoriteRecipe favorite, CancellationToken ct = default)
    {
        _db.FavoriteRecipes.Add(new FavoriteRecipeEntity
        {
            UserId = favorite.UserId,
            RecipeId = favorite.RecipeId,
            CreatedAt = favorite.CreatedAt
        });
        await _db.SaveChangesAsync(ct);
    }

    public async Task RemoveAsync(Guid userId, Guid recipeId, CancellationToken ct = default) =>
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
