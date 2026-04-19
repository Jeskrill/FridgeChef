using FridgeChef.Application.Auth.Login;
using FridgeChef.Application.Auth.Logout;
using FridgeChef.Application.Auth.RefreshToken;
using FridgeChef.Application.Auth.Register;
using FridgeChef.Application.Favorites;
using FridgeChef.Application.FoodNodes;
using FridgeChef.Application.Pantry;
using FridgeChef.Application.Pricing;
using FridgeChef.Application.Profile;
using FridgeChef.Application.Recipes.GetCatalog;
using FridgeChef.Application.Recipes.GetRecipeDetail;
using FridgeChef.Application.Recipes.MatchFromPantry;
using FridgeChef.Application.Settings;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace FridgeChef.Application.DependencyInjection;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Validators
        services.AddValidatorsFromAssemblyContaining<RegisterValidator>(ServiceLifetime.Scoped);

        // Auth
        services.AddScoped<RegisterHandler>();
        services.AddScoped<LoginHandler>();
        services.AddScoped<RefreshTokenHandler>();
        services.AddScoped<LogoutHandler>();

        // Profile
        services.AddScoped<GetProfileHandler>();
        services.AddScoped<UpdateProfileHandler>();
        services.AddScoped<ChangePasswordHandler>();

        // Recipes
        services.AddScoped<GetCatalogHandler>();
        services.AddScoped<GetRecipeDetailHandler>();
        services.AddScoped<MatchHandler>();

        // Pantry
        services.AddScoped<GetPantryItemsHandler>();
        services.AddScoped<AddPantryItemHandler>();
        services.AddScoped<UpdatePantryItemHandler>();
        services.AddScoped<RemovePantryItemHandler>();

        // Settings
        services.AddScoped<GetAllergensHandler>();
        services.AddScoped<AddAllergenHandler>();
        services.AddScoped<RemoveAllergenHandler>();
        services.AddScoped<GetFavoriteFoodsHandler>();
        services.AddScoped<AddFavoriteFoodHandler>();
        services.AddScoped<RemoveFavoriteFoodHandler>();
        services.AddScoped<GetExcludedFoodsHandler>();
        services.AddScoped<AddExcludedFoodHandler>();
        services.AddScoped<RemoveExcludedFoodHandler>();
        services.AddScoped<GetDietsHandler>();
        services.AddScoped<UpdateDietsHandler>();

        // Favorites
        services.AddScoped<GetFavoritesHandler>();
        services.AddScoped<AddFavoriteHandler>();
        services.AddScoped<RemoveFavoriteHandler>();

        // Food Nodes & Pricing
        services.AddScoped<SearchFoodNodesHandler>();
        services.AddScoped<GetFoodNodeHandler>();
        services.AddScoped<GetUnitsHandler>();
        services.AddScoped<GetTaxonsHandler>();
        services.AddScoped<GetPricesHandler>();

        return services;
    }
}
