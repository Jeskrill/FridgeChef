using FridgeChef.UserPreferences.Domain;
using FridgeChef.SharedKernel;
using FluentValidation;

namespace FridgeChef.UserPreferences.Application.UseCases;

public sealed record AllergenResponse(long FoodNodeId, string Severity);
public sealed record FavoriteFoodResponse(long FoodNodeId);
public sealed record ExcludedFoodResponse(long FoodNodeId);
public sealed record UserDietResponse(long TaxonId);
public sealed record UserCuisineResponse(long TaxonId);

public sealed record AddAllergenRequest(long FoodNodeId, string Severity = "strict");
public sealed record AddFavoriteFoodRequest(long FoodNodeId);
public sealed record AddExcludedFoodRequest(long FoodNodeId);
public sealed record UpdateDietsRequest(long[] TaxonIds);
public sealed record UpdateCuisinesRequest(long[] TaxonIds);

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
        RuleFor(x => x.TaxonIds).NotNull()
            .Must(ids => ids.All(id => id > 0))
            .Must(ids => ids.Distinct().Count() == ids.Length)
            .Must(ids => ids.Length <= 50).WithMessage("Максимум 50 диет");
    }
}
public sealed class UpdateCuisinesValidator : AbstractValidator<UpdateCuisinesRequest>
{
    public UpdateCuisinesValidator()
    {
        RuleFor(x => x.TaxonIds).NotNull()
            .Must(ids => ids.All(id => id > 0))
            .Must(ids => ids.Distinct().Count() == ids.Length)
            .Must(ids => ids.Length <= 20).WithMessage("Максимум 20 кухонных предпочтений");
    }
}

public sealed class GetAllergensHandler(IUserPreferencesRepository prefs)
{
    public async Task<IReadOnlyList<AllergenResponse>> HandleAsync(Guid userId, CancellationToken ct = default)
    {
        var list = await prefs.GetAllergensAsync(userId, ct);
        return list.Select(a => new AllergenResponse(a.FoodNodeId, a.Severity.ToString())).ToList();
    }
}
public sealed class AddAllergenHandler(IUserPreferencesRepository prefs)
{
    public async Task<Result> HandleAsync(Guid userId, AddAllergenRequest req, CancellationToken ct = default)
    {
        await prefs.AddAllergenAsync(new UserAllergen(userId, req.FoodNodeId,
            Enum.Parse<AllergenSeverity>(req.Severity, true), DateTime.UtcNow), ct);
        return Result.Success();
    }
}
public sealed class RemoveAllergenHandler(IUserPreferencesRepository prefs)
{
    public async Task<Result> HandleAsync(Guid userId, long foodNodeId, CancellationToken ct = default)
    {
        await prefs.RemoveAllergenAsync(userId, foodNodeId, ct);
        return Result.Success();
    }
}
public sealed class GetFavoriteFoodsHandler(IUserPreferencesRepository prefs)
{
    public async Task<IReadOnlyList<FavoriteFoodResponse>> HandleAsync(Guid userId, CancellationToken ct = default)
        => (await prefs.GetFavoriteFoodsAsync(userId, ct)).Select(f => new FavoriteFoodResponse(f.FoodNodeId)).ToList();
}
public sealed class AddFavoriteFoodHandler(IUserPreferencesRepository prefs)
{
    public async Task<Result> HandleAsync(Guid userId, AddFavoriteFoodRequest req, CancellationToken ct = default)
    {
        await prefs.AddFavoriteFoodAsync(new UserFavoriteFood(userId, req.FoodNodeId, 1.0m, DateTime.UtcNow), ct);
        return Result.Success();
    }
}
public sealed class RemoveFavoriteFoodHandler(IUserPreferencesRepository prefs)
{
    public async Task<Result> HandleAsync(Guid userId, long foodNodeId, CancellationToken ct = default)
    {
        await prefs.RemoveFavoriteFoodAsync(userId, foodNodeId, ct);
        return Result.Success();
    }
}
public sealed class GetExcludedFoodsHandler(IUserPreferencesRepository prefs)
{
    public async Task<IReadOnlyList<ExcludedFoodResponse>> HandleAsync(Guid userId, CancellationToken ct = default)
        => (await prefs.GetExcludedFoodsAsync(userId, ct)).Select(e => new ExcludedFoodResponse(e.FoodNodeId)).ToList();
}
public sealed class AddExcludedFoodHandler(IUserPreferencesRepository prefs)
{
    public async Task<Result> HandleAsync(Guid userId, AddExcludedFoodRequest req, CancellationToken ct = default)
    {
        await prefs.AddExcludedFoodAsync(new UserExcludedFood(userId, req.FoodNodeId, DateTime.UtcNow), ct);
        return Result.Success();
    }
}
public sealed class RemoveExcludedFoodHandler(IUserPreferencesRepository prefs)
{
    public async Task<Result> HandleAsync(Guid userId, long foodNodeId, CancellationToken ct = default)
    {
        await prefs.RemoveExcludedFoodAsync(userId, foodNodeId, ct);
        return Result.Success();
    }
}
public sealed class GetDietsHandler(IUserPreferencesRepository prefs)
{
    public async Task<IReadOnlyList<UserDietResponse>> HandleAsync(Guid userId, CancellationToken ct = default)
        => (await prefs.GetDefaultDietsAsync(userId, ct)).Select(d => new UserDietResponse(d.TaxonId)).ToList();
}
public sealed class UpdateDietsHandler(IUserPreferencesRepository prefs)
{
    public async Task<Result> HandleAsync(Guid userId, UpdateDietsRequest req, CancellationToken ct = default)
    {
        await prefs.ReplaceDefaultDietsAsync(userId, req.TaxonIds.Distinct().ToArray(), ct);
        return Result.Success();
    }
}
public sealed class GetCuisinesHandler(IUserPreferencesRepository prefs)
{
    public async Task<IReadOnlyList<UserCuisineResponse>> HandleAsync(Guid userId, CancellationToken ct = default)
        => (await prefs.GetPreferredCuisinesAsync(userId, ct)).Select(c => new UserCuisineResponse(c.TaxonId)).ToList();
}
public sealed class UpdateCuisinesHandler(IUserPreferencesRepository prefs)
{
    public async Task<Result> HandleAsync(Guid userId, UpdateCuisinesRequest req, CancellationToken ct = default)
    {
        await prefs.ReplacePreferredCuisinesAsync(userId, req.TaxonIds.Distinct().ToArray(), ct);
        return Result.Success();
    }
}
