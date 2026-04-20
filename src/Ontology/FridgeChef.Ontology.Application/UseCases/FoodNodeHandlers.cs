using FridgeChef.Ontology.Domain;
using FridgeChef.Taxonomy.Domain;
using FridgeChef.SharedKernel;

namespace FridgeChef.Ontology.Application.UseCases;

public sealed record FoodNodeSearchResponse(long Id, string CanonicalName, string? AliasText, double Similarity);
public sealed record FoodNodeResponse(long Id, string CanonicalName, string Slug, string Kind, string Status);
public sealed record UnitResponse(long Id, string Code, string Name, string? Symbol);
public sealed record TaxonResponse(long Id, string Name, string Slug, string Kind);

public sealed class SearchFoodNodesHandler(IFoodNodeRepository repo)
{
    public async Task<IReadOnlyList<FoodNodeSearchResponse>> HandleAsync(string query, CancellationToken ct = default)
    {
        var results = await repo.SearchAsync(query, 10, ct);
        return results.Select(r => new FoodNodeSearchResponse(r.Id, r.CanonicalName, r.AliasText, r.Similarity)).ToList();
    }
}

public sealed class GetFoodNodeHandler(IFoodNodeRepository repo)
{
    public async Task<Result<FoodNodeResponse>> HandleAsync(long id, CancellationToken ct = default)
    {
        var node = await repo.GetByIdAsync(id, ct);
        if (node is null) return DomainErrors.NotFound.FoodNode(id);
        return new FoodNodeResponse(node.Id, node.CanonicalName, node.Slug, node.NodeKind.ToString(), node.Status.ToString());
    }
}

public sealed class GetUnitsHandler(IUnitRepository repo)
{
    public async Task<IReadOnlyList<UnitResponse>> HandleAsync(CancellationToken ct = default)
    {
        var units = await repo.GetAllAsync(ct);
        return units.Select(u => new UnitResponse(u.Id, u.Code, u.Name, u.Symbol)).ToList();
    }
}

public sealed class GetTaxonsHandler(ITaxonRepository repo)
{
    public async Task<IReadOnlyList<TaxonResponse>> HandleAsync(TaxonKind? kind, CancellationToken ct = default)
    {
        var taxons = kind.HasValue
            ? await repo.GetByKindAsync(kind.Value, ct)
            : await repo.GetAllAsync(ct);
        return taxons.Select(t => new TaxonResponse(t.Id, t.Name, t.Slug, t.Kind.ToString())).ToList();
    }
}
