using FridgeChef.Catalog.Application.Dto;
using FridgeChef.SharedKernel;

namespace FridgeChef.Catalog.Application.UseCases.GetRecipeDetail;

public sealed class GetRecipeDetailHandler
{
    private readonly IRecipeRepository _recipes;

    public GetRecipeDetailHandler(IRecipeRepository recipes) => _recipes = recipes;

    public async Task<Result<RecipeDetailResponse>> HandleAsync(
        string slug, CancellationToken ct = default)
    {
        var detail = await _recipes.GetDetailBySlugAsync(slug, ct);

        if (detail is null)
            return DomainErrors.NotFound.RecipeBySlug(slug);

        return detail;
    }
}
