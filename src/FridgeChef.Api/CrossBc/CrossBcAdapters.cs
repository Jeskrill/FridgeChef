using FridgeChef.Catalog.Application;
using FridgeChef.Catalog.Domain;
using FridgeChef.Favorites.Application.UseCases;

namespace FridgeChef.Api.CrossBc;

internal sealed class CatalogRecipeSummaryProvider(IRecipeRepository recipes) : IRecipeSummaryProvider
{
    public async Task<IReadOnlyList<RecipeSummaryDto>> GetByIdsAsync(
        IEnumerable<Guid> ids, CancellationToken ct)
    {
        var idList = ids.ToList();
        if (idList.Count == 0) return Array.Empty<RecipeSummaryDto>();

        var result = await recipes.GetSummariesByIdsAsync(idList, ct);
        return result
            .Select(r => new RecipeSummaryDto(r.Id, r.Slug, r.Title, r.ImageUrl))
            .ToList();
    }
}

internal static class CrossBcAdapterExtensions
{
    public static IServiceCollection AddCrossBcAdapters(this IServiceCollection services)
    {
        services.AddScoped<IRecipeSummaryProvider, CatalogRecipeSummaryProvider>();
        return services;
    }
}
