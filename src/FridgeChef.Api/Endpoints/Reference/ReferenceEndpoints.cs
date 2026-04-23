using FridgeChef.Ontology.Application.UseCases;
using FridgeChef.Catalog.Application.Dto;
using FridgeChef.Api.Middleware;
using FridgeChef.Taxonomy.Domain;
using Microsoft.AspNetCore.Mvc;

namespace FridgeChef.Api.Endpoints.Reference;

internal static class ReferenceEndpoints
{
    public static void MapReferenceEndpoints(this IEndpointRouteBuilder app)
    {

        app.MapGet("/units", async (GetUnitsHandler handler, CancellationToken ct) =>
            Results.Ok(await handler.HandleAsync(ct)))
            .WithTags("Reference")
            .Produces<IReadOnlyList<UnitResponse>>()
            .WithSummary("Справочник единиц измерения")
            .WithDescription("""
                Возвращает полный список единиц измерения, доступных в системе.
                Используется при добавлении продуктов в холодильник (`POST /pantry`).

                Каждая единица содержит:
                - `id` — ID для использования в `unitId`
                - `code` — краткий код (например `g`, `ml`, `pcs`)
                - `name` — полное название на русском (например `Граммы`)
                - `symbol` — символ (например `г`, `мл`, `шт`) — может быть null

                Этот эндпоинт открытый, авторизация не нужна.
                """);

        app.MapGet("/taxons", async (string? kind, [FromServices] GetTaxonsHandler handler, CancellationToken ct) =>
        {
            if (!string.IsNullOrWhiteSpace(kind) && !Enum.TryParse<TaxonKind>(kind, true, out _))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["kind"] = ["Unsupported taxon kind. Valid values: Diet, Cuisine, Occasion, Technique"]
                });
            }

            TaxonKind? parsedKind = Enum.TryParse<TaxonKind>(kind, true, out var k) ? k : null;
            return Results.Ok(await handler.HandleAsync(parsedKind, ct));
        })
        .WithTags("Reference")
        .Produces<IReadOnlyList<TaxonResponse>>()
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .WithSummary("Справочник таксонов")
        .WithDescription("""
            Возвращает список таксонов — клазификационных тегов для рецептов.

            **Параметр `kind`** фильтрует по типу таксона:
            - `Diet` — диеты и ограничения (Веганское, Кето, Без глютена, Халяль…)
            - `Cuisine` — типы кухни (Русская, Итальянская, Японская…)
            - `Occasion` — тип блюда/повода (Завтрак, Ужин, Праздничное…)
            - `Technique` — техника приготовления (Жарка, Запекание, Варка…)

            Если `kind` не указан — возвращаются таксоны всех типов.

            **Использование:**
            Полученные `id` таксонов передаются в:
            - `GET /recipes?diet[]=123&cuisine[]=456` — фильтрация каталога
            - `POST /recipes/search` с `dietFilterIds` — подбор из холодильника
            - `PUT /settings/diets` — диеты по умолчанию пользователя

            Этот эндпоинт открытый, авторизация не нужна.
            """);
    }
}
