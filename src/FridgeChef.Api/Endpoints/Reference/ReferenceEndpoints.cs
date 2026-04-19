using FridgeChef.Application.FoodNodes;
using FridgeChef.Domain.Taxonomy;
using Microsoft.AspNetCore.Mvc;

namespace FridgeChef.Api.Endpoints.Reference;

internal static class ReferenceEndpoints
{
    public static void MapReferenceEndpoints(this IEndpointRouteBuilder app)
    {
        // Units — справочник единиц измерения
        app.MapGet("/units", async (GetUnitsHandler handler, CancellationToken ct) =>
            Results.Ok(await handler.HandleAsync(ct)))
            .WithTags("Reference")
            .Produces<IReadOnlyList<UnitResponse>>()
            .WithSummary("Справочник единиц измерения");

        // Taxons — диеты, кухни и прочие таксоны
        app.MapGet("/taxons", async (string? kind, GetTaxonsHandler handler, CancellationToken ct) =>
        {
            TaxonKind? parsedKind = Enum.TryParse<TaxonKind>(kind, true, out var k) ? k : null;
            return Results.Ok(await handler.HandleAsync(parsedKind, ct));
        })
        .WithTags("Reference")
        .Produces<IReadOnlyList<TaxonResponse>>()
        .WithSummary("Справочник таксонов (диеты, кухни и т.д.)");
    }
}
