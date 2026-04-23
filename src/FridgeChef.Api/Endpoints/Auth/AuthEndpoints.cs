using FridgeChef.Auth.Application.UseCases;
using FridgeChef.Auth.Application.Dto;
using FridgeChef.Api.Middleware;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace FridgeChef.Api.Endpoints.Auth;

internal static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth")
            .WithTags("Auth")
            .RequireRateLimiting("AuthPerIp");

        group.MapPost("/registration", async (
            HttpContext http,
            RegisterRequest request,
            IValidator<RegisterRequest> validator,
            [FromServices] RegisterHandler handler,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var clientContext = new AuthClientContext(
                http.Request.Headers.UserAgent.ToString(),
                http.Connection.RemoteIpAddress);

            var result = await handler.HandleAsync(request, clientContext, ct);
            return result.ToHttpResult(StatusCodes.Status201Created);
        })
        .Produces<AuthTokensResponse>(StatusCodes.Status201Created)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
        .WithSummary("Регистрация пользователя")
        .WithDescription("Создаёт новую учётную запись. После регистрации возвращается пара JWT-токенов.");

        group.MapPost("/sessions", async (
            HttpContext http,
            LoginRequest request,
            IValidator<LoginRequest> validator,
            [FromServices] LoginHandler handler,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var clientContext = new AuthClientContext(
                http.Request.Headers.UserAgent.ToString(),
                http.Connection.RemoteIpAddress);

            var result = await handler.HandleAsync(request, clientContext, ct);
            return result.ToHttpResult();
        })
        .Produces<AuthTokensResponse>()
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
        .WithSummary("Вход в систему")
        .WithDescription("Аутентификация по e-mail и паролю. Возвращает пару JWT-токенов.");

        group.MapPost("/tokens", async (
            HttpContext http,
            RefreshTokenRequest request,
            IValidator<RefreshTokenRequest> validator,
            [FromServices] RefreshTokenHandler handler,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var clientContext = new AuthClientContext(
                http.Request.Headers.UserAgent.ToString(),
                http.Connection.RemoteIpAddress);

            var result = await handler.HandleAsync(request, clientContext, ct);
            return result.ToHttpResult();
        })
        .Produces<AuthTokensResponse>()
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
        .WithSummary("Обновление токена")
        .WithDescription("Обменивает refresh-токен на новую пару токенов. Старый токен становится недействительным.");

        group.MapDelete("/sessions", async (
            HttpContext http,
            [FromServices] LogoutHandler handler,
            CancellationToken ct) =>
        {
            var userId = http.User.GetUserId();
            var result = await handler.HandleAsync(userId, ct);
            return result.ToHttpResult();
        })
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
        .RequireAuthorization()
        .Produces(StatusCodes.Status204NoContent)
        .WithSummary("Выход из системы")
        .WithDescription("Инвалидирует все refresh-токены пользователя. Требуется JWT-авторизация.");
    }
}
