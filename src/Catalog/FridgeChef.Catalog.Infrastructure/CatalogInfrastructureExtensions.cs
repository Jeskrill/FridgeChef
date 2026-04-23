using FridgeChef.Admin.Application.UseCases;
using FridgeChef.Catalog.Application;
using FridgeChef.Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FridgeChef.Catalog.Infrastructure;

public static class CatalogInfrastructureExtensions
{
    public static IServiceCollection AddCatalogInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<CatalogDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));

        services.AddScoped<IRecipeRepository, RecipeRepository>();

        services.AddScoped<IAdminRecipeReader, AdminRecipeAdapter>();
        services.AddScoped<IAdminRecipeWriter, AdminRecipeAdapter>();

        return services;
    }
}
