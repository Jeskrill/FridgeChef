using FridgeChef.Catalog.Application.Converters;
using FridgeChef.Catalog.Application.Dto;
using FridgeChef.Catalog.Domain;
using FridgeChef.SharedKernel;

namespace FridgeChef.Catalog.Application.UseCases.GetRecipeDetail;

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

        return recipe.ToDetailDto();
    }
}
