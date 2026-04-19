using FridgeChef.Ontology.Domain;
using FridgeChef.Ontology.Infrastructure.Persistence.Entities;

namespace FridgeChef.Ontology.Infrastructure.Persistence.Converters;

internal static class OntologyEntityConverter
{
    internal static FoodNode ToDomain(this FoodNodeEntity e) => new(
        Id: e.Id,
        CanonicalName: e.CanonicalName,
        NormalizedName: e.NormalizedName,
        Slug: e.Slug,
        NodeKind: Enum.TryParse<FoodNodeKind>(e.NodeKind, ignoreCase: true, out var nk)
            ? nk
            : FoodNodeKind.Ingredient,
        Status: Enum.TryParse<FoodNodeStatus>(e.Status, ignoreCase: true, out var st)
            ? st
            : FoodNodeStatus.Active,
        MergedIntoId: e.MergedIntoId,
        DefaultUnitId: e.DefaultUnitId,
        CreatedAt: e.CreatedAt,
        UpdatedAt: e.UpdatedAt,
        Aliases: e.Aliases.Select(a => a.ToDomain()).ToList());

    private static FoodAlias ToDomain(this FoodAliasEntity e) => new(
        Id: e.Id,
        FoodNodeId: e.FoodNodeId,
        AliasText: e.AliasText,
        AliasNormalized: e.AliasNormalized,
        AliasType: Enum.TryParse<AliasType>(e.AliasType, ignoreCase: true, out var at)
            ? at
            : AliasType.Synonym,
        LanguageCode: e.LanguageCode,
        Priority: e.Priority,
        IsPreferred: e.IsPreferred);

    internal static Unit ToDomain(this UnitEntity e) => new(
        Id: e.Id,
        Code: e.Code,
        Name: e.Name,
        Symbol: e.Symbol,
        QuantityClass: e.QuantityClass,
        ToBaseMultiplier: e.ToBaseMultiplier);
}
