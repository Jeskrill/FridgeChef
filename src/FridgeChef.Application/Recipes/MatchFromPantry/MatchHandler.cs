using FridgeChef.Application.Mapping;
using FridgeChef.Application.Recipes.Dto;
using FridgeChef.Domain.Catalog;
using FridgeChef.Domain.Ontology;
using FridgeChef.Domain.Pantry;
using FridgeChef.Domain.UserPreferences;

namespace FridgeChef.Application.Recipes.MatchFromPantry;

public sealed record MatchRequest(
    long[]? DietFilterIds = null,
    int MaxResults = 50);

public sealed record MatchResultResponse(
    RecipeCardResponse Recipe,
    double Score,
    int MatchedIngredientCount,
    int TotalIngredientCount,
    IReadOnlyList<string> MissingIngredients);

public sealed class MatchHandler
{
    private readonly IPantryRepository _pantry;
    private readonly IUserPreferencesRepository _preferences;
    private readonly IFoodHierarchyService _hierarchy;
    private readonly IRecipeRepository _recipes;

    public MatchHandler(
        IPantryRepository pantry,
        IUserPreferencesRepository preferences,
        IFoodHierarchyService hierarchy,
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
        if (pantryNodeIds.Count == 0) return Array.Empty<MatchResultResponse>();

        var expandedNodeIds = await _hierarchy.ExpandDescendantsAsync(pantryNodeIds, ct);

        var allergenNodeIds = await _preferences.GetAllergenFoodNodeIdsAsync(userId, ct);
        var expandedAllergenIds = allergenNodeIds.Count > 0
            ? await _hierarchy.GetAllergenFoodNodeIdsAsync(allergenNodeIds, ct)
            : new HashSet<long>();

        // Only apply diet filter if explicitly requested — default diets are preferences, not hard filters
        var dietTaxonIds = request.DietFilterIds;

        var candidates = await _recipes.GetByFoodNodeIdsAsync(
            expandedNodeIds,
            expandedAllergenIds.Count > 0 ? expandedAllergenIds.ToArray() : null,
            dietTaxonIds is { Length: > 0 } ? dietTaxonIds : null,
            request.MaxResults * 2,
            ct);

        var results = candidates
            .Select(recipe => ScoreRecipe(recipe, expandedNodeIds))
            .Where(r => r.Score > 0)
            .OrderByDescending(r => r.Score)
            .Take(request.MaxResults)
            .ToList();

        return results;
    }

    private static MatchResultResponse ScoreRecipe(Recipe recipe, IReadOnlySet<long> pantryNodeIds)
    {
        var ingredientsWithNodes = recipe.Ingredients
            .Where(i => i.FoodNodeId.HasValue && !i.IsOptional)
            .ToList();

        if (ingredientsWithNodes.Count == 0)
        {
            return new MatchResultResponse(
                recipe.ToCardResponse(), 0, 0,
                recipe.Ingredients.Count,
                Array.Empty<string>());
        }

        var matched = ingredientsWithNodes
            .Where(i => pantryNodeIds.Contains(i.FoodNodeId!.Value))
            .ToList();

        var missing = ingredientsWithNodes
            .Where(i => !pantryNodeIds.Contains(i.FoodNodeId!.Value))
            .Select(i => i.DisplayName)
            .ToList();

        var score = (double)matched.Count / ingredientsWithNodes.Count;

        return new MatchResultResponse(
            recipe.ToCardResponse(),
            Math.Round(score * 100, 1),
            matched.Count,
            ingredientsWithNodes.Count,
            missing);
    }
}
