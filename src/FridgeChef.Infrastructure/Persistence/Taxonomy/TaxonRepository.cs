using FridgeChef.Domain.Taxonomy;
using Microsoft.EntityFrameworkCore;

namespace FridgeChef.Infrastructure.Persistence.Taxonomy;

internal sealed class TaxonRepository : ITaxonRepository
{
    private readonly FridgeChefDbContext _db;
    public TaxonRepository(FridgeChefDbContext db) => _db = db;

    public async Task<IReadOnlyList<Taxon>> GetByKindAsync(TaxonKind kind, CancellationToken ct = default) =>
        await _db.Taxons.Where(t => t.Kind == kind).OrderBy(t => t.Name).ToListAsync(ct);

    public async Task<IReadOnlyList<Taxon>> GetAllAsync(CancellationToken ct = default) =>
        await _db.Taxons.OrderBy(t => t.Kind).ThenBy(t => t.Name).ToListAsync(ct);
}
