using FridgeChef.SharedKernel;

namespace FridgeChef.Catalog.Domain;

public sealed record RecipeSummary(Guid Id, string Slug, string Title, string? ImageUrl);
