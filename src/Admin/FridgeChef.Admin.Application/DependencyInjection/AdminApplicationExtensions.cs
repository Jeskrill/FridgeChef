using FridgeChef.Admin.Application.UseCases;
using Microsoft.Extensions.DependencyInjection;

namespace FridgeChef.Admin.Application.DependencyInjection;

public static class AdminApplicationExtensions
{
    public static IServiceCollection AddAdminApplication(this IServiceCollection services)
    {

        services.AddScoped<GetAdminUsersHandler>();
        services.AddScoped<SetUserBlockedHandler>();

        services.AddScoped<GetAdminRecipesHandler>();
        services.AddScoped<UpdateRecipeStatusHandler>();
        services.AddScoped<DeleteRecipeHandler>();

        services.AddScoped<GetAdminIngredientsHandler>();
        services.AddScoped<CreateIngredientHandler>();
        services.AddScoped<UpdateIngredientHandler>();
        services.AddScoped<DeleteIngredientHandler>();

        services.AddScoped<GetAdminTaxonsHandler>();
        services.AddScoped<CreateTaxonHandler>();
        services.AddScoped<UpdateTaxonHandler>();
        services.AddScoped<DeleteTaxonHandler>();

        services.AddScoped<GetAdminStatsHandler>();

        return services;
    }
}
