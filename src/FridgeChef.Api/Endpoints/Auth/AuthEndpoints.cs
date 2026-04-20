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
        var group = app.MapGroup("/auth")
            .WithTags("Auth")
            .RequireRateLimiting("AuthPerIp");

        group.MapPost("/register", async (
            HttpContext http,
            RegisterRequest request,
            IValidator<RegisterRequest> validator,
            RegisterHandler handler,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var clientContext = new Domain.Auth.AuthClientContext(
                http.Request.Headers.UserAgent.ToString(),
                http.Connection.RemoteIpAddress);

            var result = await handler.HandleAsync(request, clientContext, ct);
            return result.ToHttpResult(StatusCodes.Status201Created);
        });

        group.MapPost("/login", async (
            HttpContext http,
            LoginRequest request,
            IValidator<LoginRequest> validator,
            LoginHandler handler,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var clientContext = new Domain.Auth.AuthClientContext(
                http.Request.Headers.UserAgent.ToString(),
                http.Connection.RemoteIpAddress);

            var result = await handler.HandleAsync(request, clientContext, ct);
            return result.ToHttpResult();
        });

        group.MapPost("/refresh", async (
            HttpContext http,
            RefreshTokenRequest request,
            IValidator<RefreshTokenRequest> validator,
            RefreshTokenHandler handler,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var clientContext = new Domain.Auth.AuthClientContext(
                http.Request.Headers.UserAgent.ToString(),
                http.Connection.RemoteIpAddress);

            var result = await handler.HandleAsync(request, clientContext, ct);
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
