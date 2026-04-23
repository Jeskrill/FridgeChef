using FridgeChef.Catalog.Application.Dto;
using FridgeChef.SharedKernel;

namespace FridgeChef.Catalog.Application.UseCases.GetCatalog;

public sealed record GetCatalogRequest(
    string? Query = null,
    long[]? DietIds = null,
    long[]? CuisineIds = null,
    string? CuisineName = null,
    int? MaxTimeMin = null,
    decimal? MaxKcal = null,
    int Page = 1,
    int PageSize = 20);

public sealed class GetCatalogHandler
{
    private readonly IRecipeRepository _recipes;

    public GetCatalogHandler(IRecipeRepository recipes) => _recipes = recipes;

    public Task<PagedResult<RecipeCardResponse>> HandleAsync(
        GetCatalogRequest request, CancellationToken ct = default)
    {
        var paging = new PagedRequest(request.Page, request.PageSize);
        return _recipes.GetCatalogAsync(
            request.Query, request.DietIds, request.CuisineIds, paging, ct);
    }
}
