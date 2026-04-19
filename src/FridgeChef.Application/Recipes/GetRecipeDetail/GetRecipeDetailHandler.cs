using FridgeChef.Application.Mapping;
using FridgeChef.Application.Recipes.Dto;
using FridgeChef.Domain.Catalog;
using FridgeChef.Domain.Common;

namespace FridgeChef.Application.Recipes.GetRecipeDetail;

public sealed class GetRecipeDetailHandler
{
    private readonly IRecipeRepository _recipes;
    public GetRecipeDetailHandler(IRecipeRepository recipes) => _recipes = recipes;

    public async Task<Result<RecipeDetailResponse>> HandleAsync(
        string slug, CancellationToken ct = default)
    {
        var recipe = await _recipes.GetBySlugAsync(slug, ct);

        if (recipe is null)
            return DomainErrors.NotFound.RecipeBySlug(slug);

        return recipe.ToDetailResponse();
    }
}
