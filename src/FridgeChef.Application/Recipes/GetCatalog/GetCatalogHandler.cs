using FridgeChef.Application.Mapping;
using FridgeChef.Application.Recipes.Dto;
using FridgeChef.Domain.Catalog;
using FridgeChef.Domain.Common;

namespace FridgeChef.Application.Recipes.GetCatalog;

public sealed record GetCatalogRequest(
    string? Query = null,
    long[]? DietIds = null,
    long[]? CuisineIds = null,
    int Page = 1,
    int PageSize = 20);

public sealed class GetCatalogHandler
{
    private readonly IRecipeRepository _recipes;
    public GetCatalogHandler(IRecipeRepository recipes) => _recipes = recipes;

    public async Task<PagedResult<RecipeCardResponse>> HandleAsync(
        GetCatalogRequest request, CancellationToken ct = default)
    {
        var paging = new PagedRequest(request.Page, request.PageSize);
        var result = await _recipes.GetCatalogAsync(
            request.Query, request.DietIds, request.CuisineIds, paging, ct);
        var cards = result.Items.Select(r => r.ToCardResponse()).ToList();
        return new PagedResult<RecipeCardResponse>(
            cards, result.TotalCount, result.Page, result.PageSize);
    }
}
