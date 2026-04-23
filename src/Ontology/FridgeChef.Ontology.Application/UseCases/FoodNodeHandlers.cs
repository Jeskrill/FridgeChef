using FridgeChef.SharedKernel;
using FridgeChef.Taxonomy.Domain;

namespace FridgeChef.Ontology.Application.UseCases;

public sealed record FoodNodeSearchResponse(long Id, string CanonicalName, string? AliasText, double Similarity);
public sealed record FoodNodeResponse(long Id, string CanonicalName, string Slug, string Kind, string Status);
public sealed record UnitResponse(long Id, string Code, string Name, string? Symbol);
public sealed record TaxonResponse(long Id, string Name, string Slug, string Kind);

public interface IFoodNodeRepository
{
    Task<IReadOnlyList<FoodNodeSearchResponse>> SearchAsync(string query, int limit = 10, CancellationToken ct = default);
    Task<FoodNodeResponse?> GetByIdAsync(long id, CancellationToken ct = default);
}

public interface IUnitRepository
{
    Task<IReadOnlyList<UnitResponse>> GetAllAsync(CancellationToken ct = default);
}

public interface ITaxonRepository
{
    Task<IReadOnlyList<TaxonResponse>> GetByKindAsync(TaxonKind kind, CancellationToken ct = default);
    Task<IReadOnlyList<TaxonResponse>> GetAllAsync(CancellationToken ct = default);
}

public sealed class SearchFoodNodesHandler(IFoodNodeRepository repo)
{
    public Task<IReadOnlyList<FoodNodeSearchResponse>> HandleAsync(string query, CancellationToken ct = default)
        => repo.SearchAsync(query, 10, ct);
}

public sealed class GetFoodNodeHandler(IFoodNodeRepository repo)
{
    public async Task<Result<FoodNodeResponse>> HandleAsync(long id, CancellationToken ct = default)
    {
        var node = await repo.GetByIdAsync(id, ct);
        if (node is null) return DomainErrors.NotFound.FoodNode(id);
        return node;
    }
}

public sealed class GetUnitsHandler(IUnitRepository repo)
{
    public Task<IReadOnlyList<UnitResponse>> HandleAsync(CancellationToken ct = default)
        => repo.GetAllAsync(ct);
}

public sealed class GetTaxonsHandler(ITaxonRepository repo)
{
    public Task<IReadOnlyList<TaxonResponse>> HandleAsync(TaxonKind? kind, CancellationToken ct = default)
        => kind.HasValue ? repo.GetByKindAsync(kind.Value, ct) : repo.GetAllAsync(ct);
}
