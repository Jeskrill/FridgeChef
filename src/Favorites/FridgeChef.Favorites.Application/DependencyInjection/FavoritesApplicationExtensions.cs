using FridgeChef.Favorites.Application.UseCases;
using Microsoft.Extensions.DependencyInjection;

namespace FridgeChef.Favorites.Application.DependencyInjection;

public static class FavoritesApplicationExtensions
{
    public static IServiceCollection AddFavoritesApplication(this IServiceCollection services)
    {
        services.AddScoped<GetFavoritesHandler>();
        services.AddScoped<AddFavoriteHandler>();
        services.AddScoped<RemoveFavoriteHandler>();
        return services;
    }
}
