using FluentValidation;
using FridgeChef.Catalog.Application.UseCases.GetCatalog;
using FridgeChef.Catalog.Application.UseCases.GetRecipeDetail;
using FridgeChef.Catalog.Application.UseCases.MatchFromPantry;
using Microsoft.Extensions.DependencyInjection;

namespace FridgeChef.Catalog.Application;

public static class CatalogApplicationExtensions
{
    public static IServiceCollection AddCatalogApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<MatchRequestValidator>(ServiceLifetime.Scoped);

        services.AddScoped<GetCatalogHandler>();
        services.AddScoped<GetRecipeDetailHandler>();
        services.AddScoped<MatchFromPantryHandler>();

        return services;
    }
}
