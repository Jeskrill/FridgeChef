using FluentValidation;
using FridgeChef.Pantry.Domain;
using FridgeChef.SharedKernel;

namespace FridgeChef.Pantry.Application.UseCases;

public sealed record PantryItemResponse(
    Guid Id, long FoodNodeId, decimal? Quantity,
    long? UnitId, QuantityMode QuantityMode, DateTime CreatedAt);

public sealed record AddPantryItemRequest(long FoodNodeId, decimal? Quantity, long? UnitId);
public sealed record UpdatePantryItemRequest(decimal? Quantity, long? UnitId);

public interface IPantryRepository
{
    Task<IReadOnlyList<PantryItemResponse>> GetByUserIdAsync(Guid userId, CancellationToken ct);
    Task<PantryItemResponse?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<bool> ExistsAsync(Guid userId, long foodNodeId, CancellationToken ct);
    Task<bool> ExistsByIdAndUserAsync(Guid id, Guid userId, CancellationToken ct);
    Task<PantryItemResponse> AddAsync(Guid userId, AddPantryItemRequest request, CancellationToken ct);
    Task<PantryItemResponse> UpdateAsync(Guid id, UpdatePantryItemRequest request, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
    Task<IReadOnlySet<long>> GetFoodNodeIdsByUserAsync(Guid userId, CancellationToken ct);
}

public sealed class AddPantryItemValidator : AbstractValidator<AddPantryItemRequest>
{
    public AddPantryItemValidator()
    {
        RuleFor(x => x.FoodNodeId).GreaterThan(0).WithMessage("Food node ID должен быть положительным");
        When(x => x.Quantity.HasValue, () =>
            RuleFor(x => x.Quantity!.Value).GreaterThan(0));
        RuleFor(x => x)
            .Must(x => !x.UnitId.HasValue || x.Quantity.HasValue)
            .WithMessage("Unit ID можно передать только вместе с количеством");
    }
}

public sealed class UpdatePantryItemValidator : AbstractValidator<UpdatePantryItemRequest>
{
    public UpdatePantryItemValidator()
    {
        RuleFor(x => x)
            .Must(x => x.Quantity.HasValue || x.UnitId.HasValue)
            .WithMessage("Нужно передать хотя бы одно поле для обновления");
        When(x => x.Quantity.HasValue, () =>
            RuleFor(x => x.Quantity!.Value).GreaterThan(0));
    }
}

public sealed class GetPantryItemsHandler(IPantryRepository pantry)
{
    public Task<IReadOnlyList<PantryItemResponse>> HandleAsync(
        Guid userId, CancellationToken ct)
        => pantry.GetByUserIdAsync(userId, ct);
}

public sealed class AddPantryItemHandler(IPantryRepository pantry)
{
    public async Task<Result<PantryItemResponse>> HandleAsync(
        Guid userId, AddPantryItemRequest req, CancellationToken ct)
    {
        var exists = await pantry.ExistsAsync(userId, req.FoodNodeId, ct);
        if (exists) return DomainErrors.Pantry.AlreadyExists;

        var response = await pantry.AddAsync(userId, req, ct);
        return response;
    }
}

public sealed class UpdatePantryItemHandler(IPantryRepository pantry)
{
    public async Task<Result<PantryItemResponse>> HandleAsync(
        Guid userId, Guid itemId, UpdatePantryItemRequest req, CancellationToken ct)
    {
        if (!await pantry.ExistsByIdAndUserAsync(itemId, userId, ct))
            return DomainErrors.NotFound.PantryItem(itemId);

        var updated = await pantry.UpdateAsync(itemId, req, ct);
        return updated;
    }
}

public sealed class RemovePantryItemHandler(IPantryRepository pantry)
{
    public async Task<Result> HandleAsync(Guid userId, Guid itemId, CancellationToken ct)
    {
        if (!await pantry.ExistsByIdAndUserAsync(itemId, userId, ct))
            return DomainErrors.NotFound.PantryItem(itemId);

        await pantry.DeleteAsync(itemId, ct);
        return Result.Success();
    }
}
