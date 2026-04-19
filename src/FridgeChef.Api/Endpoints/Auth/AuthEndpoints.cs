using FridgeChef.Application.Auth.Login;
using FridgeChef.Application.Auth.Logout;
using FridgeChef.Application.Auth.RefreshToken;
using FridgeChef.Application.Auth.Register;
using FridgeChef.Api.Middleware;
using FluentValidation;

namespace FridgeChef.Api.Endpoints.Auth;

internal static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth").WithTags("Auth");

        group.MapPost("/register", async (
            RegisterRequest request,
            IValidator<RegisterRequest> validator,
            RegisterHandler handler,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var result = await handler.HandleAsync(request, ct);
            return result.ToHttpResult(StatusCodes.Status201Created);
        });

        group.MapPost("/login", async (
            LoginRequest request,
            LoginHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(request, ct);
            return result.ToHttpResult();
        });

        group.MapPost("/refresh", async (
            RefreshTokenRequest request,
            RefreshTokenHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(request, ct);
            return result.ToHttpResult();
        });

        group.MapPost("/logout", async (
            HttpContext http,
            LogoutHandler handler,
            CancellationToken ct) =>
        {
            var userId = http.User.GetUserId();
            var result = await handler.HandleAsync(userId, ct);
            return result.ToHttpResult();
        }).RequireAuthorization();
    }
}
