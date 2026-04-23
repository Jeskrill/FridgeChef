using FridgeChef.Auth.Application.UseCases;
using FridgeChef.SharedKernel;

namespace FridgeChef.Admin.Application.UseCases;

public sealed record AdminUserResponse(
    Guid Id, string DisplayName, string Email,
    string Role, bool IsBlocked, DateTime? LastLoginAt, DateTime CreatedAt);
public sealed record AdminUserListResponse(
    IReadOnlyList<AdminUserResponse> Users, int TotalCount, int Page, int PageSize);
public sealed record SetUserBlockedRequest(bool IsBlocked);

public sealed record AdminRecipeResponse(
    Guid Id, string Slug, string Title, string? Cuisine,
    int? TimeMin, decimal? EstimatedCost, string Status, DateTime CreatedAt);
public sealed record AdminRecipeListResponse(
    IReadOnlyList<AdminRecipeResponse> Recipes, int TotalCount, int Page, int PageSize);
public sealed record SetRecipeStatusRequest(string Status);

public sealed record AdminIngredientResponse(
    long Id, string CanonicalName, string Slug, string Kind,
    string Status, string? DefaultUnit, long? DefaultUnitId, DateTime CreatedAt);
public sealed record AdminIngredientListResponse(
    IReadOnlyList<AdminIngredientResponse> Ingredients, int TotalCount, int Page, int PageSize);
public sealed record CreateIngredientRequest(string CanonicalName, string Kind, long? DefaultUnitId);
public sealed record UpdateIngredientRequest(string? CanonicalName, string? Kind, long? DefaultUnitId);

public sealed record AdminTaxonResponse(long Id, string Kind, string Name, string Slug, string? Description, int RecipeCount);
public sealed record AdminTaxonListResponse(
    IReadOnlyList<AdminTaxonResponse> Taxons, int TotalCount);
public sealed record CreateTaxonRequest(string Kind, string Name, string? Description);
public sealed record UpdateTaxonRequest(string? Name, string? Description);

public sealed record AdminStatsResponse(
    int TotalUsers, int TotalRecipes, int TotalFavorites,
    IReadOnlyList<AdminPopularRecipeResponse> PopularRecipes);
public sealed record AdminPopularRecipeResponse(
    Guid Id, string Slug, string Title, string? ImageUrl, int FavoriteCount);

public interface IAdminUserReader
{
    Task<IReadOnlyList<AdminUserResponse>> GetAllAsync(string? query, CancellationToken ct = default);
    Task<int> CountAsync(CancellationToken ct = default);
}

public interface IAdminRecipeReader
{
    Task<int> CountAsync(CancellationToken ct = default);
    Task<AdminPopularRecipeResponse?> GetSummaryByIdAsync(Guid id, CancellationToken ct = default);
    Task<AdminRecipeListResponse> GetPagedAsync(string? query, int page, int pageSize, CancellationToken ct = default);
}

public interface IAdminRecipeWriter
{
    Task<bool> UpdateStatusAsync(Guid id, string status, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}

public interface IAdminIngredientReader
{
    Task<AdminIngredientListResponse> GetPagedAsync(string? query, int page, int pageSize, CancellationToken ct = default);
    Task<AdminIngredientResponse?> GetByIdAsync(long id, CancellationToken ct = default);
}

public interface IAdminIngredientWriter
{
    Task<AdminIngredientResponse> CreateAsync(CreateIngredientRequest req, CancellationToken ct = default);
    Task<AdminIngredientResponse?> UpdateAsync(long id, UpdateIngredientRequest req, CancellationToken ct = default);
    Task<bool> DeleteAsync(long id, CancellationToken ct = default);
}

public interface IAdminTaxonReader
{
    Task<AdminTaxonListResponse> GetAllAsync(string? kind, CancellationToken ct = default);
    Task<AdminTaxonResponse?> GetByIdAsync(long id, CancellationToken ct = default);
}

public interface IAdminTaxonWriter
{
    Task<AdminTaxonResponse> CreateAsync(CreateTaxonRequest req, CancellationToken ct = default);
    Task<AdminTaxonResponse?> UpdateAsync(long id, UpdateTaxonRequest req, CancellationToken ct = default);
    Task<bool> DeleteAsync(long id, CancellationToken ct = default);
}

public interface IAdminFavoritesReader
{
    Task<int> CountTotalAsync(CancellationToken ct = default);
    Task<IReadOnlyList<(Guid RecipeId, int Count)>> GetMostFavoritedAsync(int limit, CancellationToken ct = default);
}

public sealed class GetAdminUsersHandler(IAdminUserReader users)
{
    public async Task<AdminUserListResponse> HandleAsync(
        string? query, int page, int pageSize, CancellationToken ct = default)
    {
        var allUsers = await users.GetAllAsync(query, ct);
        var total    = allUsers.Count;
        var paged    = allUsers
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToList();
        return new AdminUserListResponse(paged, total, page, pageSize);
    }
}

public sealed class SetUserBlockedHandler(IUserRepository users)
{
    public async Task<Result<AdminUserResponse>> HandleAsync(
        Guid userId, SetUserBlockedRequest request, CancellationToken ct = default)
    {
        var user = await users.GetByIdAsync(userId, ct);
        if (user is null) return DomainErrors.NotFound.User(userId);

        var updated = user with { IsBlocked = request.IsBlocked, UpdatedAt = DateTime.UtcNow };
        await users.UpdateAsync(updated, ct);
        return new AdminUserResponse(updated.Id, updated.DisplayName, updated.Email, updated.Role, updated.IsBlocked, updated.LastLoginAt, updated.CreatedAt);
    }
}

public sealed class GetAdminRecipesHandler(IAdminRecipeReader recipes)
{
    public Task<AdminRecipeListResponse> HandleAsync(
        string? query, int page, int pageSize, CancellationToken ct = default)
        => recipes.GetPagedAsync(query, page, pageSize, ct);
}

public sealed class UpdateRecipeStatusHandler(IAdminRecipeWriter recipes)
{
    private static readonly HashSet<string> ValidStatuses = ["published", "draft", "archived"];

    public async Task<Result> HandleAsync(
        Guid id, SetRecipeStatusRequest request, CancellationToken ct = default)
    {
        var status = request.Status.Trim().ToLowerInvariant();
        if (!ValidStatuses.Contains(status))
            return DomainErrors.Validation("Status", "Допустимые статусы: published, draft, archived");

        var success = await recipes.UpdateStatusAsync(id, status, ct);
        if (!success) return DomainErrors.NotFound.Recipe(id);
        return Result.Success();
    }
}

public sealed class DeleteRecipeHandler(IAdminRecipeWriter recipes)
{
    public async Task<Result> HandleAsync(Guid id, CancellationToken ct = default)
    {
        var success = await recipes.DeleteAsync(id, ct);
        if (!success) return DomainErrors.NotFound.Recipe(id);
        return Result.Success();
    }
}

public sealed class GetAdminIngredientsHandler(IAdminIngredientReader ingredients)
{
    public Task<AdminIngredientListResponse> HandleAsync(
        string? query, int page, int pageSize, CancellationToken ct = default)
        => ingredients.GetPagedAsync(query, page, pageSize, ct);
}

public sealed class CreateIngredientHandler(IAdminIngredientWriter ingredients)
{
    public async Task<Result<AdminIngredientResponse>> HandleAsync(
        CreateIngredientRequest req, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(req.CanonicalName))
            return DomainErrors.Validation("CanonicalName", "Название обязательно");

        var result = await ingredients.CreateAsync(req, ct);
        return result;
    }
}

public sealed class UpdateIngredientHandler(IAdminIngredientWriter ingredients)
{
    public async Task<Result<AdminIngredientResponse>> HandleAsync(
        long id, UpdateIngredientRequest req, CancellationToken ct = default)
    {
        var result = await ingredients.UpdateAsync(id, req, ct);
        if (result is null) return DomainErrors.NotFound.FoodNode(id);
        return result;
    }
}

public sealed class DeleteIngredientHandler(IAdminIngredientWriter ingredients)
{
    public async Task<Result> HandleAsync(long id, CancellationToken ct = default)
    {
        var success = await ingredients.DeleteAsync(id, ct);
        if (!success) return DomainErrors.NotFound.FoodNode(id);
        return Result.Success();
    }
}

public sealed class GetAdminTaxonsHandler(IAdminTaxonReader taxons)
{
    public Task<AdminTaxonListResponse> HandleAsync(
        string? kind, CancellationToken ct = default)
        => taxons.GetAllAsync(kind, ct);
}

public sealed class CreateTaxonHandler(IAdminTaxonWriter taxons)
{
    public async Task<Result<AdminTaxonResponse>> HandleAsync(
        CreateTaxonRequest req, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            return DomainErrors.Validation("Name", "Название обязательно");
        if (string.IsNullOrWhiteSpace(req.Kind))
            return DomainErrors.Validation("Kind", "Тип таксона обязателен");

        var result = await taxons.CreateAsync(req, ct);
        return result;
    }
}

public sealed class UpdateTaxonHandler(IAdminTaxonWriter taxons)
{
    public async Task<Result<AdminTaxonResponse>> HandleAsync(
        long id, UpdateTaxonRequest req, CancellationToken ct = default)
    {
        var result = await taxons.UpdateAsync(id, req, ct);
        if (result is null) return DomainErrors.NotFound.Taxon(id);
        return result;
    }
}

public sealed class DeleteTaxonHandler(IAdminTaxonWriter taxons)
{
    public async Task<Result> HandleAsync(long id, CancellationToken ct = default)
    {
        var success = await taxons.DeleteAsync(id, ct);
        if (!success) return DomainErrors.NotFound.Taxon(id);
        return Result.Success();
    }
}

public sealed class GetAdminStatsHandler(
    IAdminUserReader users,
    IAdminRecipeReader recipes,
    IAdminFavoritesReader favorites)
{
    public async Task<AdminStatsResponse> HandleAsync(CancellationToken ct = default)
    {
        var totalUsers   = await users.CountAsync(ct);
        var totalRecipes = await recipes.CountAsync(ct);
        var totalFavs    = await favorites.CountTotalAsync(ct);
        var topRecipes   = await favorites.GetMostFavoritedAsync(5, ct);

        var popular = new List<AdminPopularRecipeResponse>();
        foreach (var (recipeId, count) in topRecipes)
        {
            var summary = await recipes.GetSummaryByIdAsync(recipeId, ct);
            if (summary is not null)
                popular.Add(summary with { FavoriteCount = count });
        }

        return new AdminStatsResponse(totalUsers, totalRecipes, totalFavs, popular);
    }
}
