using FluentValidation;
using FridgeChef.UserPreferences.Application.UseCases;
using Microsoft.Extensions.DependencyInjection;

namespace FridgeChef.UserPreferences.Application.DependencyInjection;

public static class UserPreferencesApplicationExtensions
{
    public static IServiceCollection AddUserPreferencesApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<AddAllergenValidator>(ServiceLifetime.Scoped);
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
        services.AddScoped<GetCuisinesHandler>();
        services.AddScoped<UpdateCuisinesHandler>();
        return services;
    }
}
