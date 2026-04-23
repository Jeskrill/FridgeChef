using FridgeChef.Catalog.Application.UseCases.MatchFromPantry;
using FridgeChef.UserPreferences.Application.UseCases;
using FridgeChef.UserPreferences.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FridgeChef.UserPreferences.Infrastructure;

internal sealed class UserPreferredCuisineEntity
{
    public Guid UserId { get; set; }
    public long TaxonId { get; set; }
    public DateTime CreatedAt { get; set; }
}
internal sealed class UserAllergenEntity
{
    public Guid UserId { get; set; }
    public long FoodNodeId { get; set; }
    public string Severity { get; set; } = "strict";
    public DateTime CreatedAt { get; set; }
}

internal sealed class UserExcludedFoodEntity
{
    public Guid UserId { get; set; }
    public long FoodNodeId { get; set; }
    public DateTime CreatedAt { get; set; }
}

internal sealed class UserFavoriteFoodEntity
{
    public Guid UserId { get; set; }
    public long FoodNodeId { get; set; }
    public decimal Weight { get; set; } = 1.0m;
    public DateTime CreatedAt { get; set; }
}

internal sealed class UserDefaultDietEntity
{
    public Guid UserId { get; set; }
    public long TaxonId { get; set; }
    public DateTime CreatedAt { get; set; }
}

internal sealed class UserPreferencesDbContext : DbContext
{
    public UserPreferencesDbContext(DbContextOptions<UserPreferencesDbContext> options) : base(options)
    {
        ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTrackingWithIdentityResolution;
    }

    internal DbSet<UserAllergenEntity> UserAllergens => Set<UserAllergenEntity>();
    internal DbSet<UserExcludedFoodEntity> UserExcludedFoods => Set<UserExcludedFoodEntity>();
    internal DbSet<UserFavoriteFoodEntity> UserFavoriteFoods => Set<UserFavoriteFoodEntity>();
    internal DbSet<UserDefaultDietEntity> UserDefaultDiets => Set<UserDefaultDietEntity>();
    internal DbSet<UserPreferredCuisineEntity> UserPreferredCuisines => Set<UserPreferredCuisineEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserAllergenEntity>().ToTable("user_allergens", "user_domain").HasKey(e => new { e.UserId, e.FoodNodeId });
        modelBuilder.Entity<UserAllergenEntity>().Property(e => e.UserId).HasColumnName("user_id");
        modelBuilder.Entity<UserAllergenEntity>().Property(e => e.FoodNodeId).HasColumnName("food_node_id");
        modelBuilder.Entity<UserAllergenEntity>().Property(e => e.Severity).HasColumnName("severity");
        modelBuilder.Entity<UserAllergenEntity>().Property(e => e.CreatedAt).HasColumnName("created_at");

        modelBuilder.Entity<UserExcludedFoodEntity>().ToTable("user_excluded_foods", "user_domain").HasKey(e => new { e.UserId, e.FoodNodeId });
        modelBuilder.Entity<UserExcludedFoodEntity>().Property(e => e.UserId).HasColumnName("user_id");
        modelBuilder.Entity<UserExcludedFoodEntity>().Property(e => e.FoodNodeId).HasColumnName("food_node_id");
        modelBuilder.Entity<UserExcludedFoodEntity>().Property(e => e.CreatedAt).HasColumnName("created_at");

        modelBuilder.Entity<UserFavoriteFoodEntity>().ToTable("user_favorite_foods", "user_domain").HasKey(e => new { e.UserId, e.FoodNodeId });
        modelBuilder.Entity<UserFavoriteFoodEntity>().Property(e => e.UserId).HasColumnName("user_id");
        modelBuilder.Entity<UserFavoriteFoodEntity>().Property(e => e.FoodNodeId).HasColumnName("food_node_id");
        modelBuilder.Entity<UserFavoriteFoodEntity>().Property(e => e.Weight).HasColumnName("weight").HasDefaultValue(1.0m);
        modelBuilder.Entity<UserFavoriteFoodEntity>().Property(e => e.CreatedAt).HasColumnName("created_at");

        modelBuilder.Entity<UserDefaultDietEntity>().ToTable("user_default_diets", "user_domain").HasKey(e => new { e.UserId, e.TaxonId });
        modelBuilder.Entity<UserDefaultDietEntity>().Property(e => e.UserId).HasColumnName("user_id");
        modelBuilder.Entity<UserDefaultDietEntity>().Property(e => e.TaxonId).HasColumnName("taxon_id");
        modelBuilder.Entity<UserDefaultDietEntity>().Property(e => e.CreatedAt).HasColumnName("created_at");

        modelBuilder.Entity<UserPreferredCuisineEntity>().ToTable("user_preferred_cuisines", "user_domain").HasKey(e => new { e.UserId, e.TaxonId });
        modelBuilder.Entity<UserPreferredCuisineEntity>().Property(e => e.UserId).HasColumnName("user_id");
        modelBuilder.Entity<UserPreferredCuisineEntity>().Property(e => e.TaxonId).HasColumnName("taxon_id");
        modelBuilder.Entity<UserPreferredCuisineEntity>().Property(e => e.CreatedAt).HasColumnName("created_at");
    }
}

internal sealed class UserPreferencesRepository : IUserPreferencesRepository
{
    private readonly UserPreferencesDbContext _db;
    public UserPreferencesRepository(UserPreferencesDbContext db) => _db = db;

    public async Task<IReadOnlyList<AllergenResponse>> GetAllergensAsync(Guid userId, CancellationToken ct = default)
    {
        var entities = await _db.UserAllergens.Where(a => a.UserId == userId).ToListAsync(ct);
        return entities.Select(e => new AllergenResponse(e.FoodNodeId, e.Severity)).ToList();
    }

    public async Task AddAllergenAsync(Guid userId, AddAllergenRequest request, CancellationToken ct = default)
    {
        _db.UserAllergens.Add(new UserAllergenEntity
        {
            UserId = userId,
            FoodNodeId = request.FoodNodeId,
            Severity = request.Severity.ToLowerInvariant(),
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);
    }

    public async Task RemoveAllergenAsync(Guid userId, long foodNodeId, CancellationToken ct = default) =>
        await _db.UserAllergens.Where(a => a.UserId == userId && a.FoodNodeId == foodNodeId).ExecuteDeleteAsync(ct);

    public async Task<IReadOnlyList<FavoriteFoodResponse>> GetFavoriteFoodsAsync(Guid userId, CancellationToken ct = default)
    {
        var entities = await _db.UserFavoriteFoods.Where(f => f.UserId == userId).ToListAsync(ct);
        return entities.Select(e => new FavoriteFoodResponse(e.FoodNodeId)).ToList();
    }

    public async Task AddFavoriteFoodAsync(Guid userId, long foodNodeId, CancellationToken ct = default)
    {
        _db.UserFavoriteFoods.Add(new UserFavoriteFoodEntity { UserId = userId, FoodNodeId = foodNodeId, Weight = 1.0m, CreatedAt = DateTime.UtcNow });
        await _db.SaveChangesAsync(ct);
    }

    public async Task RemoveFavoriteFoodAsync(Guid userId, long foodNodeId, CancellationToken ct = default) =>
        await _db.UserFavoriteFoods.Where(f => f.UserId == userId && f.FoodNodeId == foodNodeId).ExecuteDeleteAsync(ct);

    public async Task<IReadOnlyList<ExcludedFoodResponse>> GetExcludedFoodsAsync(Guid userId, CancellationToken ct = default)
    {
        var entities = await _db.UserExcludedFoods.Where(e => e.UserId == userId).ToListAsync(ct);
        return entities.Select(e => new ExcludedFoodResponse(e.FoodNodeId)).ToList();
    }

    public async Task AddExcludedFoodAsync(Guid userId, long foodNodeId, CancellationToken ct = default)
    {
        _db.UserExcludedFoods.Add(new UserExcludedFoodEntity { UserId = userId, FoodNodeId = foodNodeId, CreatedAt = DateTime.UtcNow });
        await _db.SaveChangesAsync(ct);
    }

    public async Task RemoveExcludedFoodAsync(Guid userId, long foodNodeId, CancellationToken ct = default) =>
        await _db.UserExcludedFoods.Where(e => e.UserId == userId && e.FoodNodeId == foodNodeId).ExecuteDeleteAsync(ct);

    public async Task<IReadOnlyList<UserDietResponse>> GetDefaultDietsAsync(Guid userId, CancellationToken ct = default)
    {
        var entities = await _db.UserDefaultDiets.Where(d => d.UserId == userId).ToListAsync(ct);
        return entities.Select(e => new UserDietResponse(e.TaxonId)).ToList();
    }

    public async Task ReplaceDefaultDietsAsync(Guid userId, IReadOnlyList<long> taxonIds, CancellationToken ct = default)
    {
        await _db.UserDefaultDiets.Where(d => d.UserId == userId).ExecuteDeleteAsync(ct);
        var newDiets = taxonIds.Select(id => new UserDefaultDietEntity { UserId = userId, TaxonId = id, CreatedAt = DateTime.UtcNow });
        _db.UserDefaultDiets.AddRange(newDiets);
        await _db.SaveChangesAsync(ct);
    }

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

    public async Task<IReadOnlyList<UserCuisineResponse>> GetPreferredCuisinesAsync(Guid userId, CancellationToken ct = default)
    {
        var entities = await _db.UserPreferredCuisines.Where(c => c.UserId == userId).ToListAsync(ct);
        return entities.Select(e => new UserCuisineResponse(e.TaxonId)).ToList();
    }

    public async Task ReplacePreferredCuisinesAsync(Guid userId, IReadOnlyList<long> taxonIds, CancellationToken ct = default)
    {
        await _db.UserPreferredCuisines.Where(c => c.UserId == userId).ExecuteDeleteAsync(ct);
        var newCuisines = taxonIds.Select(id => new UserPreferredCuisineEntity { UserId = userId, TaxonId = id, CreatedAt = DateTime.UtcNow });
        _db.UserPreferredCuisines.AddRange(newCuisines);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlySet<long>> GetPreferredCuisineTaxonIdsAsync(Guid userId, CancellationToken ct = default)
    {
        var ids = await _db.UserPreferredCuisines.Where(c => c.UserId == userId).Select(c => c.TaxonId).ToListAsync(ct);
        return ids.ToHashSet();
    }
}

internal sealed class UserPreferencesSupplierAdapter : IUserPreferencesSupplier
{
    private readonly IUserPreferencesRepository _prefs;
    public UserPreferencesSupplierAdapter(IUserPreferencesRepository prefs) => _prefs = prefs;

    public Task<IReadOnlySet<long>> GetAllergenFoodNodeIdsAsync(Guid userId, CancellationToken ct = default) =>
        _prefs.GetAllergenFoodNodeIdsAsync(userId, ct);
}

public static class UserPreferencesInfrastructureExtensions
{
    public static IServiceCollection AddUserPreferencesInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<UserPreferencesDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IUserPreferencesRepository, UserPreferencesRepository>();

        services.AddScoped<IUserPreferencesSupplier, UserPreferencesSupplierAdapter>();

        return services;
    }
}
