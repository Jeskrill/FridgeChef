using FridgeChef.Api.Endpoints.Auth;
using FridgeChef.Api.Endpoints.Favorites;
using FridgeChef.Api.Endpoints.FoodNodes;
using FridgeChef.Api.Endpoints.Pantry;
using FridgeChef.Api.Endpoints.Pricing;
using FridgeChef.Api.Endpoints.Profile;
using FridgeChef.Api.Endpoints.Recipes;
using FridgeChef.Api.Endpoints.Reference;
using FridgeChef.Api.Endpoints.Settings;

namespace FridgeChef.Api.Extensions;

internal static class EndpointExtensions
{
    public static void MapAllEndpoints(this WebApplication app)
    {
        app.MapAuthEndpoints();
        app.MapProfileEndpoints();
        app.MapRecipeEndpoints();
        app.MapPantryEndpoints();
        app.MapFavoriteEndpoints();
        app.MapSettingsEndpoints();
        app.MapFoodNodeEndpoints();
        app.MapReferenceEndpoints();
        app.MapPricingEndpoints();
    }
}
