using FridgeChef.Domain.Auth;
using FridgeChef.Domain.Catalog;
using FridgeChef.Domain.Favorites;
using FridgeChef.Domain.Ontology;
using FridgeChef.Domain.Pantry;
using FridgeChef.Domain.Pricing;
using FridgeChef.Domain.Taxonomy;
using FridgeChef.Domain.UserPreferences;
using FridgeChef.Infrastructure.Persistence;
using FridgeChef.Infrastructure.Persistence.Auth;
using FridgeChef.Infrastructure.Persistence.Catalog;
using FridgeChef.Infrastructure.Persistence.Ontology;
using FridgeChef.Infrastructure.Persistence.Pricing;
using FridgeChef.Infrastructure.Persistence.Taxonomy;
using FridgeChef.Infrastructure.Persistence.UserDomain;
using FridgeChef.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FridgeChef.Infrastructure.DependencyInjection;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        services.AddDbContext<FridgeChefDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IRecipeRepository, RecipeRepository>();
        services.AddScoped<IFoodNodeRepository, FoodNodeRepository>();
        services.AddScoped<IUnitRepository, UnitRepository>();
        services.AddScoped<IFoodHierarchyService, FoodHierarchyService>();
        services.AddScoped<ITaxonRepository, TaxonRepository>();
        services.AddScoped<IPantryRepository, PantryRepository>();
        services.AddScoped<IUserPreferencesRepository, UserPreferencesRepository>();
        services.AddScoped<IFavoriteRecipeRepository, FavoriteRecipeRepository>();
        services.AddScoped<IPricingRepository, PricingRepository>();

        // Security
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        return services;
    }
}
