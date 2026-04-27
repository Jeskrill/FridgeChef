using FluentValidation;
using FridgeChef.Api.Middleware;
using FridgeChef.Auth.Application.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace FridgeChef.Api.Endpoints.Profile;

internal static class ProfileEndpoints
{
    public static void MapProfileEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/users")
            .WithTags("Profile")
            .RequireAuthorization();

        group.MapGet("/me", async (HttpContext http, [FromServices] GetProfileHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(http.User.GetUserId(), ct);
            return result.ToHttpResult();
        })
        .Produces<UserProfileResponse>()
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
        .WithSummary("Профиль пользователя")
        .WithDescription("""
            Возвращает публичные данные текущего авторизованного пользователя:
            ID, имя, e-mail, аватар, роль (`user` / `admin`), дата регистрации.
            Требуется JWT-авторизация.
            """);

        group.MapPatch("/me", async (
            HttpContext http,
            UpdateProfileRequest request,
            IValidator<UpdateProfileRequest> validator,
            [FromServices] UpdateProfileHandler handler,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var result = await handler.HandleAsync(http.User.GetUserId(), request, ct);
            return result.ToHttpResult();
        })
        .Produces<UserProfileResponse>()
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
        .WithSummary("Обновление профиля")
        .WithDescription("""
            Частичное обновление профиля. Необходимо передать хотя бы одно поле.
            - `displayName` — отображаемое имя (не более 100 символов)
            - `email` — новый e-mail (должен быть уникальным в системе)

            Требуется JWT-авторизация.
            """);

        group.MapPut("/me/password", async (
            HttpContext http,
            ChangePasswordRequest request,
            IValidator<ChangePasswordRequest> validator,
            [FromServices] ChangePasswordHandler handler,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var result = await handler.HandleAsync(http.User.GetUserId(), request, ct);
            return result.ToHttpResult();
        })
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
        .WithSummary("Смена пароля")
        .WithDescription("""
            Изменяет пароль текущего пользователя.
            После смены все существующие refresh-токены инвалидируются на всех устройствах.

            - `oldPassword` — текущий пароль для подтверждения
            - `newPassword` — новый пароль (не менее 8 символов, отличается от текущего)

            Требуется JWT-авторизация.
            """);
    }
}
