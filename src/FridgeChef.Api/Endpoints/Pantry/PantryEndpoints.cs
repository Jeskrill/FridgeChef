using FridgeChef.Pantry.Application.UseCases;
using FridgeChef.Api.Middleware;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace FridgeChef.Api.Endpoints.Pantry;

internal static class PantryEndpoints
{
    public static void MapPantryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/pantry")
            .WithTags("Pantry")
            .RequireAuthorization();

        group.MapGet("/", async (HttpContext http, [FromServices] GetPantryItemsHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(http.User.GetUserId(), ct);
            return Results.Ok(result);
        })
        .Produces<IReadOnlyList<PantryItemResponse>>()
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
        .WithSummary("Содержимое холодильника")
        .WithDescription("""
            Возвращает список продуктов, добавленных пользователем в холодильник.

            Каждый элемент содержит:
            - `foodNodeId` — ID продукта из онтологии (`GET /food-nodes?q=...`)
            - `quantity` — количество (может отсутствовать)
            - `unitId` — ID единицы измерения (`GET /units`)
            - `quantityMode` — режим: `Exact`, `PackageDefault`, `CountOnly`, `Unknown`

            Требуется JWT-авторизация.
            """);

        group.MapPost("/", async (
            HttpContext http,
            AddPantryItemRequest request,
            IValidator<AddPantryItemRequest> validator,
            [FromServices] AddPantryItemHandler handler,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var result = await handler.HandleAsync(http.User.GetUserId(), request, ct);
            return result.ToHttpResult(StatusCodes.Status201Created);
        })
        .Produces<PantryItemResponse>(StatusCodes.Status201Created)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
        .WithSummary("Добавить продукт в холодильник")
        .WithDescription("""
            Добавляет продукт в холодильник пользователя.
            При попытке добавить уже существующий `foodNodeId` возвращается `409 Conflict`.

            Поиск `foodNodeId` осуществляется через `GET /food-nodes?q=название`.
            Список единиц измерения доступен по `GET /units`.

            Требуется JWT-авторизация.
            """);

        group.MapPatch("/{id:guid}", async (
            Guid id, HttpContext http,
            UpdatePantryItemRequest request,
            IValidator<UpdatePantryItemRequest> validator,
            [FromServices] UpdatePantryItemHandler handler,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var result = await handler.HandleAsync(http.User.GetUserId(), id, request, ct);
            return result.ToHttpResult();
        })
        .Produces<PantryItemResponse>()
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
        .WithSummary("Обновить количество продукта")
        .WithDescription("""
            Частичное обновление записи в холодильнике. Необходимо передать хотя бы одно поле.
            Возвращает `404`, если запись не найдена или принадлежит другому пользователю.
            Требуется JWT-авторизация.
            """);

        group.MapDelete("/{id:guid}", async (
            Guid id, HttpContext http,
            [FromServices] RemovePantryItemHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(http.User.GetUserId(), id, ct);
            return result.ToHttpResult();
        })
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
        .WithSummary("Удалить продукт из холодильника")
        .WithDescription("""
            Удаляет запись из холодильника.
            Возвращает `404`, если запись не найдена или принадлежит другому пользователю.
            Требуется JWT-авторизация.
            """);
    }
}
