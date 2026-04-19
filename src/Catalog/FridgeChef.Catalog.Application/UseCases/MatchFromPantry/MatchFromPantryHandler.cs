using FridgeChef.Catalog.Application.Converters;
using FridgeChef.Catalog.Application.Dto;
using FridgeChef.Catalog.Domain;

namespace FridgeChef.Catalog.Application.UseCases.MatchFromPantry;

public sealed record MatchRequest(
    long[]? DietFilterIds = null,
    int MaxResults = 50);

public sealed record MatchResultResponse(
    RecipeCardResponse Recipe,
    double Score,
    int MatchedIngredientCount,
    int TotalIngredientCount,
    IReadOnlyList<string> MissingIngredients);

/// <summary>
/// Matches recipes to user's pantry using food node hierarchy expansion.
/// Depends on pantry, preferences, ontology (via interfaces defined in their respective Domains).
/// </summary>
public sealed class MatchFromPantryHandler
{
    private readonly IPantrySupplier _pantry;
    private readonly IUserPreferencesSupplier _preferences;
    private readonly IFoodHierarchySupplier _hierarchy;
    private readonly IRecipeRepository _recipes;

    public MatchFromPantryHandler(
        IPantrySupplier pantry,
        IUserPreferencesSupplier preferences,
        IFoodHierarchySupplier hierarchy,
        IRecipeRepository recipes)
    {
        _pantry = pantry;
        _preferences = preferences;
        _hierarchy = hierarchy;
        _recipes = recipes;
    }

    public async Task<IReadOnlyList<MatchResultResponse>> HandleAsync(
        Guid userId, MatchRequest request, CancellationToken ct = default)
    {
        var pantryNodeIds = await _pantry.GetFoodNodeIdsByUserAsync(userId, ct);
        if (pantryNodeIds.Count == 0) return [];

        var expandedNodeIds = await _hierarchy.ExpandDescendantsAsync(pantryNodeIds, ct);

        var allergenNodeIds = await _preferences.GetAllergenFoodNodeIdsAsync(userId, ct);
        var expandedAllergenIds = allergenNodeIds.Count > 0
            ? await _hierarchy.GetAllergenFoodNodeIdsAsync(allergenNodeIds, ct)
            : (IReadOnlySet<long>)new HashSet<long>();

        var dietTaxonIds = request.DietFilterIds;

        var candidates = await _recipes.GetByFoodNodeIdsAsync(
            expandedNodeIds,
            expandedAllergenIds.Count > 0 ? expandedAllergenIds.ToArray() : null,
            dietTaxonIds is { Length: > 0 } ? dietTaxonIds : null,
            request.MaxResults * 2,
            ct);

        return candidates
            .Select(recipe => ScoreRecipe(recipe, expandedNodeIds))
            .Where(r => r.Score > 0)
            .OrderByDescending(r => r.Score)
            .Take(request.MaxResults)
            .ToList();
    }

    private static MatchResultResponse ScoreRecipe(Recipe recipe, IReadOnlySet<long> pantryNodeIds)
    {
        var required = recipe.Ingredients
            .Where(i => i.FoodNodeId.HasValue && !i.IsOptional)
            .ToList();

        if (required.Count == 0)
            return new MatchResultResponse(recipe.ToCardDto(), 0, 0, recipe.Ingredients.Count, []);

        var matched = required.Where(i => pantryNodeIds.Contains(i.FoodNodeId!.Value)).ToList();
        var missing = required
            .Where(i => !pantryNodeIds.Contains(i.FoodNodeId!.Value))
            .Select(i => i.DisplayName)
            .ToList();

        var score = Math.Round((double)matched.Count / required.Count * 100, 1);

        return new MatchResultResponse(recipe.ToCardDto(), score, matched.Count, required.Count, missing);
    }
}

// ────────────────────────────────────────────────────────────────────
//  Cross-BC supplier interfaces (anti-corruption layer)
//  These keep Catalog.Application independent from Pantry/Preferences/Ontology.
//  Each BC's Infrastructure implements its own supplier.
// ────────────────────────────────────────────────────────────────────

/// <summary>Abstracts pantry data access for the match use case.</summary>
public interface IPantrySupplier
{
    Task<IReadOnlySet<long>> GetFoodNodeIdsByUserAsync(Guid userId, CancellationToken ct = default);
}

/// <summary>Abstracts user preferences access for the match use case.</summary>
public interface IUserPreferencesSupplier
{
    Task<IReadOnlySet<long>> GetAllergenFoodNodeIdsAsync(Guid userId, CancellationToken ct = default);
}

/// <summary>Abstracts food hierarchy expansion for the match use case.</summary>
public interface IFoodHierarchySupplier
{
    Task<IReadOnlySet<long>> ExpandDescendantsAsync(IEnumerable<long> foodNodeIds, CancellationToken ct = default);
    Task<IReadOnlySet<long>> GetAllergenFoodNodeIdsAsync(IEnumerable<long> allergenNodeIds, CancellationToken ct = default);
}
