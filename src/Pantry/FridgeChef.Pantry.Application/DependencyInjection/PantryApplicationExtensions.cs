using FridgeChef.Pantry.Application.UseCases;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace FridgeChef.Pantry.Application.DependencyInjection;

public static class PantryApplicationExtensions
{
    public static IServiceCollection AddPantryApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<AddPantryItemValidator>(ServiceLifetime.Scoped);
        services.AddScoped<GetPantryItemsHandler>();
        services.AddScoped<AddPantryItemHandler>();
        services.AddScoped<UpdatePantryItemHandler>();
        services.AddScoped<RemovePantryItemHandler>();
        return services;
    }
}
