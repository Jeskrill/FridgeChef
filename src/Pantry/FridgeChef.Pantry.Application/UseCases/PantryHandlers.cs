using FridgeChef.Pantry.Domain;
using FridgeChef.SharedKernel;
using FluentValidation;

namespace FridgeChef.Pantry.Application.UseCases;

public sealed record PantryItemResponse(
    Guid Id, long FoodNodeId, decimal? Quantity,
    long? UnitId, string QuantityMode, DateTime CreatedAt);

public sealed record AddPantryItemRequest(long FoodNodeId, decimal? Quantity, long? UnitId);
public sealed record UpdatePantryItemRequest(decimal? Quantity, long? UnitId);

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
    public async Task<IReadOnlyList<PantryItemResponse>> HandleAsync(
        Guid userId, CancellationToken ct = default)
    {
        var items = await pantry.GetByUserIdAsync(userId, ct);
        return items.Select(Map).ToList();
    }
    private static PantryItemResponse Map(PantryItem i) =>
        new(i.Id, i.FoodNodeId, i.QuantityValue, i.UnitId, i.QuantityMode.ToString(), i.CreatedAt);
}

public sealed class AddPantryItemHandler(IPantryRepository pantry)
{
    public async Task<Result<PantryItemResponse>> HandleAsync(
        Guid userId, AddPantryItemRequest req, CancellationToken ct = default)
    {
        var exists = await pantry.ExistsAsync(userId, req.FoodNodeId, ct);
        if (exists) return DomainErrors.Pantry.AlreadyExists;

        var item = new PantryItem(
            Guid.NewGuid(), userId, req.FoodNodeId,
            req.Quantity, req.UnitId,
            req.UnitId.HasValue ? QuantityMode.Exact : QuantityMode.Unknown,
            null, null, "manual", null, null,
            DateTime.UtcNow, DateTime.UtcNow);
        await pantry.AddAsync(item, ct);
        return new PantryItemResponse(item.Id, item.FoodNodeId, item.QuantityValue, item.UnitId, item.QuantityMode.ToString(), item.CreatedAt);
    }
}

public sealed class UpdatePantryItemHandler(IPantryRepository pantry)
{
    public async Task<Result<PantryItemResponse>> HandleAsync(
        Guid userId, Guid itemId, UpdatePantryItemRequest req, CancellationToken ct = default)
    {
        var item = await pantry.GetByIdAsync(itemId, ct);
        if (item is null || item.UserId != userId) return DomainErrors.NotFound.PantryItem(itemId);

        var updated = item with {
            QuantityValue = req.Quantity ?? item.QuantityValue,
            UnitId        = req.UnitId   ?? item.UnitId,
            UpdatedAt     = DateTime.UtcNow
        };
        await pantry.UpdateAsync(updated, ct);
        return new PantryItemResponse(updated.Id, updated.FoodNodeId, updated.QuantityValue, updated.UnitId, updated.QuantityMode.ToString(), updated.CreatedAt);
    }
}

public sealed class RemovePantryItemHandler(IPantryRepository pantry)
{
    public async Task<Result> HandleAsync(Guid userId, Guid itemId, CancellationToken ct = default)
    {
        var item = await pantry.GetByIdAsync(itemId, ct);
        if (item is null || item.UserId != userId) return DomainErrors.NotFound.PantryItem(itemId);
        await pantry.DeleteAsync(itemId, ct);
        return Result.Success();
    }
}
