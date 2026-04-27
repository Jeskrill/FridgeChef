using FridgeChef.SharedKernel;
using FridgeChef.Ontology.Domain;

namespace FridgeChef.Ontology.Application.UseCases;

public sealed record FoodNodeSearchResponse(long Id, string CanonicalName, string? AliasText, double Similarity);
public sealed record FoodNodeResponse(long Id, string CanonicalName, string Slug, string Kind, string Status);
public sealed record UnitResponse(long Id, string Code, string Name, string? Symbol);
public sealed record TaxonResponse(long Id, string Name, string Slug, string Kind);

public interface IFoodNodeRepository
{
    Task<IReadOnlyList<FoodNodeSearchResponse>> SearchAsync(string query, int limit, CancellationToken ct);
    Task<FoodNodeResponse?> GetByIdAsync(long id, CancellationToken ct);
}

public interface IUnitRepository
{
    Task<IReadOnlyList<UnitResponse>> GetAllAsync(CancellationToken ct);
}

public interface ITaxonRepository
{
    Task<IReadOnlyList<TaxonResponse>> GetByKindAsync(TaxonKind kind, CancellationToken ct);
    Task<IReadOnlyList<TaxonResponse>> GetAllAsync(CancellationToken ct);
}

public sealed class SearchFoodNodesHandler(IFoodNodeRepository repo)
{
    public Task<IReadOnlyList<FoodNodeSearchResponse>> HandleAsync(string query, CancellationToken ct)
        => repo.SearchAsync(query, 10, ct);
}

public sealed class GetFoodNodeHandler(IFoodNodeRepository repo)
{
    public async Task<Result<FoodNodeResponse>> HandleAsync(long id, CancellationToken ct)
    {
        var node = await repo.GetByIdAsync(id, ct);
        if (node is null) return DomainErrors.NotFound.FoodNode(id);
        return node;
    }
}

public sealed class GetUnitsHandler(IUnitRepository repo)
{
    public Task<IReadOnlyList<UnitResponse>> HandleAsync(CancellationToken ct)
        => repo.GetAllAsync(ct);
}

public sealed class GetTaxonsHandler(ITaxonRepository repo)
{
    public Task<IReadOnlyList<TaxonResponse>> HandleAsync(TaxonKind? kind, CancellationToken ct)
        => kind.HasValue ? repo.GetByKindAsync(kind.Value, ct) : repo.GetAllAsync(ct);
}
