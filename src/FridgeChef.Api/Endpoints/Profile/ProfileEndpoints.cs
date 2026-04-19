using FridgeChef.Application.Profile;
using FridgeChef.Api.Middleware;

namespace FridgeChef.Api.Endpoints.Profile;

internal static class ProfileEndpoints
{
    public static void MapProfileEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/users").WithTags("Profile").RequireAuthorization();

        group.MapGet("/me", async (HttpContext http, GetProfileHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(http.User.GetUserId(), ct);
            return result.ToHttpResult();
        });

        group.MapPatch("/me", async (
            HttpContext http,
            UpdateProfileRequest request,
            UpdateProfileHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(http.User.GetUserId(), request, ct);
            return result.ToHttpResult();
        });

        group.MapPost("/me/change-password", async (
            HttpContext http,
            ChangePasswordRequest request,
            ChangePasswordHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(http.User.GetUserId(), request, ct);
            return result.ToHttpResult();
        });
    }
}
