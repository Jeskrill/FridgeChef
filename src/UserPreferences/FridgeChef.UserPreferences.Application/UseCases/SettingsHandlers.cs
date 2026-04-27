using FluentValidation;
using FridgeChef.SharedKernel;
using FridgeChef.UserPreferences.Domain;

namespace FridgeChef.UserPreferences.Application.UseCases;

public sealed record AllergenResponse(long FoodNodeId, string Severity);
public sealed record FavoriteFoodResponse(long FoodNodeId);
public sealed record ExcludedFoodResponse(long FoodNodeId);
public sealed record UserDietResponse(long TaxonId);
public sealed record UserCuisineResponse(long TaxonId);

public sealed record AddAllergenRequest(long FoodNodeId, string Severity);
public sealed record AddFavoriteFoodRequest(long FoodNodeId);
public sealed record AddExcludedFoodRequest(long FoodNodeId);
public sealed record UpdateDietsRequest(long[] TaxonIds);
public sealed record UpdateCuisinesRequest(long[] TaxonIds);

public interface IUserPreferencesRepository
{
    Task<IReadOnlyList<AllergenResponse>> GetAllergensAsync(Guid userId, CancellationToken ct);
    Task AddAllergenAsync(Guid userId, AddAllergenRequest request, CancellationToken ct);
    Task RemoveAllergenAsync(Guid userId, long foodNodeId, CancellationToken ct);

    Task<IReadOnlyList<FavoriteFoodResponse>> GetFavoriteFoodsAsync(Guid userId, CancellationToken ct);
    Task AddFavoriteFoodAsync(Guid userId, long foodNodeId, CancellationToken ct);
    Task RemoveFavoriteFoodAsync(Guid userId, long foodNodeId, CancellationToken ct);

    Task<IReadOnlyList<ExcludedFoodResponse>> GetExcludedFoodsAsync(Guid userId, CancellationToken ct);
    Task AddExcludedFoodAsync(Guid userId, long foodNodeId, CancellationToken ct);
    Task RemoveExcludedFoodAsync(Guid userId, long foodNodeId, CancellationToken ct);

    Task<IReadOnlyList<UserDietResponse>> GetDefaultDietsAsync(Guid userId, CancellationToken ct);
    Task ReplaceDefaultDietsAsync(Guid userId, IReadOnlyList<long> taxonIds, CancellationToken ct);

    Task<IReadOnlyList<UserCuisineResponse>> GetPreferredCuisinesAsync(Guid userId, CancellationToken ct);
    Task ReplacePreferredCuisinesAsync(Guid userId, IReadOnlyList<long> taxonIds, CancellationToken ct);

    Task<IReadOnlySet<long>> GetAllergenFoodNodeIdsAsync(Guid userId, CancellationToken ct);
    Task<IReadOnlySet<long>> GetFavoriteFoodNodeIdsAsync(Guid userId, CancellationToken ct);
    Task<IReadOnlySet<long>> GetDefaultDietTaxonIdsAsync(Guid userId, CancellationToken ct);
    Task<IReadOnlySet<long>> GetPreferredCuisineTaxonIdsAsync(Guid userId, CancellationToken ct);
}

public sealed class AddAllergenValidator : AbstractValidator<AddAllergenRequest>
{
    public AddAllergenValidator()
    {
        RuleFor(x => x.FoodNodeId).GreaterThan(0).WithMessage("Food node ID должен быть положительным");
        RuleFor(x => x.Severity)
            .Must(s => Enum.TryParse<AllergenSeverity>(s, true, out _))
            .WithMessage("Severity: strict или mild");
    }
}
public sealed class AddFavoriteFoodValidator : AbstractValidator<AddFavoriteFoodRequest>
{
    public AddFavoriteFoodValidator() => RuleFor(x => x.FoodNodeId).GreaterThan(0);
}
public sealed class AddExcludedFoodValidator : AbstractValidator<AddExcludedFoodRequest>
{
    public AddExcludedFoodValidator() => RuleFor(x => x.FoodNodeId).GreaterThan(0);
}
public sealed class UpdateDietsValidator : AbstractValidator<UpdateDietsRequest>
{
    public UpdateDietsValidator()
    {
        RuleFor(x => x.TaxonIds)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .Must(ids => ids.All(id => id > 0))
            .Must(ids => ids.Distinct().Count() == ids.Length)
            .Must(ids => ids.Length <= 50).WithMessage("Максимум 50 диет");
    }
}
public sealed class UpdateCuisinesValidator : AbstractValidator<UpdateCuisinesRequest>
{
    public UpdateCuisinesValidator()
    {
        RuleFor(x => x.TaxonIds)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .Must(ids => ids.All(id => id > 0))
            .Must(ids => ids.Distinct().Count() == ids.Length)
            .Must(ids => ids.Length <= 20).WithMessage("Максимум 20 кухонных предпочтений");
    }
}

public sealed class GetAllergensHandler(IUserPreferencesRepository prefs)
{
    public Task<IReadOnlyList<AllergenResponse>> HandleAsync(Guid userId, CancellationToken ct)
        => prefs.GetAllergensAsync(userId, ct);
}
public sealed class AddAllergenHandler(IUserPreferencesRepository prefs)
{
    public async Task<Result> HandleAsync(Guid userId, AddAllergenRequest req, CancellationToken ct)
    {
        await prefs.AddAllergenAsync(userId, req, ct);
        return Result.Success();
    }
}
public sealed class RemoveAllergenHandler(IUserPreferencesRepository prefs)
{
    public async Task<Result> HandleAsync(Guid userId, long foodNodeId, CancellationToken ct)
    {
        await prefs.RemoveAllergenAsync(userId, foodNodeId, ct);
        return Result.Success();
    }
}
public sealed class GetFavoriteFoodsHandler(IUserPreferencesRepository prefs)
{
    public Task<IReadOnlyList<FavoriteFoodResponse>> HandleAsync(Guid userId, CancellationToken ct)
        => prefs.GetFavoriteFoodsAsync(userId, ct);
}
public sealed class AddFavoriteFoodHandler(IUserPreferencesRepository prefs)
{
    public async Task<Result> HandleAsync(Guid userId, AddFavoriteFoodRequest req, CancellationToken ct)
    {
        await prefs.AddFavoriteFoodAsync(userId, req.FoodNodeId, ct);
        return Result.Success();
    }
}
public sealed class RemoveFavoriteFoodHandler(IUserPreferencesRepository prefs)
{
    public async Task<Result> HandleAsync(Guid userId, long foodNodeId, CancellationToken ct)
    {
        await prefs.RemoveFavoriteFoodAsync(userId, foodNodeId, ct);
        return Result.Success();
    }
}
public sealed class GetExcludedFoodsHandler(IUserPreferencesRepository prefs)
{
    public Task<IReadOnlyList<ExcludedFoodResponse>> HandleAsync(Guid userId, CancellationToken ct)
        => prefs.GetExcludedFoodsAsync(userId, ct);
}
public sealed class AddExcludedFoodHandler(IUserPreferencesRepository prefs)
{
    public async Task<Result> HandleAsync(Guid userId, AddExcludedFoodRequest req, CancellationToken ct)
    {
        await prefs.AddExcludedFoodAsync(userId, req.FoodNodeId, ct);
        return Result.Success();
    }
}
public sealed class RemoveExcludedFoodHandler(IUserPreferencesRepository prefs)
{
    public async Task<Result> HandleAsync(Guid userId, long foodNodeId, CancellationToken ct)
    {
        await prefs.RemoveExcludedFoodAsync(userId, foodNodeId, ct);
        return Result.Success();
    }
}
public sealed class GetDietsHandler(IUserPreferencesRepository prefs)
{
    public Task<IReadOnlyList<UserDietResponse>> HandleAsync(Guid userId, CancellationToken ct)
        => prefs.GetDefaultDietsAsync(userId, ct);
}
public sealed class UpdateDietsHandler(IUserPreferencesRepository prefs)
{
    public async Task<Result> HandleAsync(Guid userId, UpdateDietsRequest req, CancellationToken ct)
    {
        await prefs.ReplaceDefaultDietsAsync(userId, req.TaxonIds.Distinct().ToArray(), ct);
        return Result.Success();
    }
}
public sealed class GetCuisinesHandler(IUserPreferencesRepository prefs)
{
    public Task<IReadOnlyList<UserCuisineResponse>> HandleAsync(Guid userId, CancellationToken ct)
        => prefs.GetPreferredCuisinesAsync(userId, ct);
}
public sealed class UpdateCuisinesHandler(IUserPreferencesRepository prefs)
{
    public async Task<Result> HandleAsync(Guid userId, UpdateCuisinesRequest req, CancellationToken ct)
    {
        await prefs.ReplacePreferredCuisinesAsync(userId, req.TaxonIds.Distinct().ToArray(), ct);
        return Result.Success();
    }
}
