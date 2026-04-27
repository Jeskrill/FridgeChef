using FridgeChef.Catalog.Application.Dto;
using FridgeChef.SharedKernel;

namespace FridgeChef.Catalog.Application.UseCases.GetCatalog;

public sealed record GetCatalogRequest(
    string? Query,
    long[]? DietIds,
    long[]? CuisineIds,
    string? CuisineName,
    int? MaxTimeMin,
    decimal? MaxKcal,
    int Page,
    int PageSize);

public sealed class GetCatalogHandler
{
    private readonly IRecipeRepository _recipes;

    public GetCatalogHandler(IRecipeRepository recipes) => _recipes = recipes;

    public Task<PagedResult<RecipeCardResponse>> HandleAsync(
        GetCatalogRequest request, CancellationToken ct)
    {
        var paging = new PagedRequest(request.Page, request.PageSize);
        return _recipes.GetCatalogAsync(
            request.Query, request.DietIds, request.CuisineIds,
            request.CuisineName, request.MaxTimeMin, request.MaxKcal,
            paging, ct);
    }
}
