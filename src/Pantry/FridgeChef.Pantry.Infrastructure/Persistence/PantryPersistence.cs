using FridgeChef.Catalog.Application.UseCases.MatchFromPantry;
using FridgeChef.Pantry.Application.UseCases;
using FridgeChef.Pantry.Domain;
using FridgeChef.Pantry.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace FridgeChef.Pantry.Infrastructure.Persistence;

internal sealed class PantryDbContext : DbContext
{
    public PantryDbContext(DbContextOptions<PantryDbContext> options) : base(options)
    {
        ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTrackingWithIdentityResolution;
    }

    internal DbSet<PantryItemEntity> PantryItems => Set<PantryItemEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PantryDbContext).Assembly);

        var builder = modelBuilder.Entity<PantryItemEntity>();
        builder.ToTable("pantry_items", "user_domain");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id");
        builder.Property(p => p.UserId).HasColumnName("user_id");
        builder.Property(p => p.FoodNodeId).HasColumnName("food_node_id");
        builder.Property(p => p.QuantityValue).HasColumnName("quantity_value");
        builder.Property(p => p.UnitId).HasColumnName("unit_id");
        builder.Property(p => p.QuantityMode).HasColumnName("quantity_mode");
        builder.Property(p => p.NormalizedAmountG).HasColumnName("normalized_amount_g");
        builder.Property(p => p.NormalizedAmountMl).HasColumnName("normalized_amount_ml");
        builder.Property(p => p.Source).HasColumnName("source").HasDefaultValue("manual");
        builder.Property(p => p.Note).HasColumnName("note");
        builder.Property(p => p.ExpiresAt).HasColumnName("expires_at");
        builder.Property(p => p.CreatedAt).HasColumnName("created_at");
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at");
    }
}

internal sealed class PantryRepository : IPantryRepository
{
    private readonly PantryDbContext _db;
    public PantryRepository(PantryDbContext db) => _db = db;

    public async Task<IReadOnlyList<PantryItemResponse>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        var entities = await _db.PantryItems
            .Where(p => p.UserId == userId)
            .OrderBy(p => p.CreatedAt)
            .ToListAsync(ct);
        return entities.Select(ToDto).ToList();
    }

    public async Task<PantryItemResponse?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var e = await _db.PantryItems.FirstOrDefaultAsync(p => p.Id == id, ct);
        return e is null ? null : ToDto(e);
    }

    public async Task<bool> ExistsAsync(Guid userId, long foodNodeId, CancellationToken ct = default) =>
        await _db.PantryItems.AnyAsync(p => p.UserId == userId && p.FoodNodeId == foodNodeId, ct);

    public async Task<PantryItemResponse> AddAsync(Guid userId, AddPantryItemRequest request, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var entity = new PantryItemEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FoodNodeId = request.FoodNodeId,
            QuantityValue = request.Quantity,
            UnitId = request.UnitId,
            QuantityMode = request.UnitId.HasValue ? "exact" : "unknown",
            Source = "manual",
            CreatedAt = now,
            UpdatedAt = now
        };
        _db.PantryItems.Add(entity);
        await _db.SaveChangesAsync(ct);
        return ToDto(entity);
    }

    public async Task<PantryItemResponse> UpdateAsync(Guid id, UpdatePantryItemRequest request, CancellationToken ct = default)
    {
        var entity = await _db.PantryItems.FirstAsync(p => p.Id == id, ct);
        if (request.Quantity.HasValue) entity.QuantityValue = request.Quantity;
        if (request.UnitId.HasValue) entity.UnitId = request.UnitId;
        entity.UpdatedAt = DateTime.UtcNow;
        _db.PantryItems.Update(entity);
        await _db.SaveChangesAsync(ct);
        return ToDto(entity);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default) =>
        await _db.PantryItems.Where(p => p.Id == id).ExecuteDeleteAsync(ct);

    public async Task<IReadOnlySet<long>> GetFoodNodeIdsByUserAsync(Guid userId, CancellationToken ct = default)
    {
        var ids = await _db.PantryItems
            .Where(p => p.UserId == userId)
            .Select(p => p.FoodNodeId)
            .ToListAsync(ct);
        return ids.ToHashSet();
    }

    private static PantryItemResponse ToDto(PantryItemEntity e) => new(
        Id: e.Id,
        FoodNodeId: e.FoodNodeId,
        Quantity: e.QuantityValue,
        UnitId: e.UnitId,
        QuantityMode: e.QuantityMode ?? "unknown",
        CreatedAt: e.CreatedAt);
}

internal sealed class PantrySupplierAdapter : IPantrySupplier
{
    private readonly IPantryRepository _pantry;
    public PantrySupplierAdapter(IPantryRepository pantry) => _pantry = pantry;

    public Task<IReadOnlySet<long>> GetFoodNodeIdsByUserAsync(Guid userId, CancellationToken ct = default) =>
        _pantry.GetFoodNodeIdsByUserAsync(userId, ct);
}
