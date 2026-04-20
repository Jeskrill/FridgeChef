using FridgeChef.Application.Profile;
using FridgeChef.Api.Middleware;
using FluentValidation;

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
            IValidator<UpdateProfileRequest> validator,
            UpdateProfileHandler handler,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var result = await handler.HandleAsync(http.User.GetUserId(), request, ct);
            return result.ToHttpResult();
        });

        group.MapPost("/me/change-password", async (
            HttpContext http,
            ChangePasswordRequest request,
            IValidator<ChangePasswordRequest> validator,
            ChangePasswordHandler handler,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var result = await handler.HandleAsync(http.User.GetUserId(), request, ct);
            return result.ToHttpResult();
        });
    }
}
