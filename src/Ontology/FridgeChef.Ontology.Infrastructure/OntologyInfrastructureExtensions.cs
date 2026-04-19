using FridgeChef.Catalog.Application.UseCases.MatchFromPantry;
using FridgeChef.Ontology.Domain;
using FridgeChef.Ontology.Infrastructure.Persistence;
using FridgeChef.Pantry.Domain;
using FridgeChef.Taxonomy.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FridgeChef.Ontology.Infrastructure;

public static class OntologyInfrastructureExtensions
{
    public static IServiceCollection AddOntologyInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<OntologyDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IFoodNodeRepository, FoodNodeRepository>();
        services.AddScoped<IUnitRepository, UnitRepository>();
        services.AddScoped<IFoodHierarchyRepository, FoodHierarchyRepository>();
        services.AddScoped<ITaxonRepository, TaxonRepository>();

        // Register cross-BC supplier adapters for MatchFromPantry use case
        services.AddScoped<IFoodHierarchySupplier, FoodHierarchySupplierAdapter>();

        return services;
    }
}

/// <summary>
/// Adapter: wraps IFoodHierarchyRepository to satisfy IFoodHierarchySupplier (from Catalog.Application).
/// Keeps BC coupling at the DI registration level, not at the interface level.
/// </summary>
internal sealed class FoodHierarchySupplierAdapter : IFoodHierarchySupplier
{
    private readonly IFoodHierarchyRepository _hierarchy;

    public FoodHierarchySupplierAdapter(IFoodHierarchyRepository hierarchy) =>
        _hierarchy = hierarchy;

    public Task<IReadOnlySet<long>> ExpandDescendantsAsync(
        IEnumerable<long> foodNodeIds, CancellationToken ct = default) =>
        _hierarchy.ExpandDescendantsAsync(foodNodeIds, ct);

    public Task<IReadOnlySet<long>> GetAllergenFoodNodeIdsAsync(
        IEnumerable<long> allergenNodeIds, CancellationToken ct = default) =>
        _hierarchy.GetAllergenFoodNodeIdsAsync(allergenNodeIds, ct);
}
