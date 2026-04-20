using FridgeChef.Ontology.Application.UseCases;
using Microsoft.Extensions.DependencyInjection;

namespace FridgeChef.Ontology.Application.DependencyInjection;

public static class OntologyApplicationExtensions
{
    public static IServiceCollection AddOntologyApplication(this IServiceCollection services)
    {
        services.AddScoped<SearchFoodNodesHandler>();
        services.AddScoped<GetFoodNodeHandler>();
        services.AddScoped<GetUnitsHandler>();
        services.AddScoped<GetTaxonsHandler>();
        return services;
    }
}
