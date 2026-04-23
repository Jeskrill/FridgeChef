using FridgeChef.Pantry.Domain;
using FridgeChef.SharedKernel;
using FluentValidation;

namespace FridgeChef.Pantry.Application.UseCases;

public sealed record PantryItemResponse(
    Guid Id, long FoodNodeId, decimal? Quantity,
    long? UnitId, string QuantityMode, DateTime CreatedAt);

public sealed record AddPantryItemRequest(long FoodNodeId, decimal? Quantity, long? UnitId);
public sealed record UpdatePantryItemRequest(decimal? Quantity, long? UnitId);

public interface IPantryRepository
{
    Task<IReadOnlyList<PantryItemResponse>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<PantryItemResponse?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid userId, long foodNodeId, CancellationToken ct = default);
    Task<PantryItemResponse> AddAsync(Guid userId, AddPantryItemRequest request, CancellationToken ct = default);
    Task<PantryItemResponse> UpdateAsync(Guid id, UpdatePantryItemRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlySet<long>> GetFoodNodeIdsByUserAsync(Guid userId, CancellationToken ct = default);
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
        Guid userId, CancellationToken ct = default)
        => pantry.GetByUserIdAsync(userId, ct);
}

public sealed class AddPantryItemHandler(IPantryRepository pantry)
{
    public async Task<Result<PantryItemResponse>> HandleAsync(
        Guid userId, AddPantryItemRequest req, CancellationToken ct = default)
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
        Guid userId, Guid itemId, UpdatePantryItemRequest req, CancellationToken ct = default)
    {
        var item = await pantry.GetByIdAsync(itemId, ct);
        if (item is null || !await pantry.ExistsAsync(userId, item.FoodNodeId, ct))
            return DomainErrors.NotFound.PantryItem(itemId);

        var updated = await pantry.UpdateAsync(itemId, req, ct);
        return updated;
    }
}

public sealed class RemovePantryItemHandler(IPantryRepository pantry)
{
    public async Task<Result> HandleAsync(Guid userId, Guid itemId, CancellationToken ct = default)
    {
        var item = await pantry.GetByIdAsync(itemId, ct);
        if (item is null) return DomainErrors.NotFound.PantryItem(itemId);

        var userItems = await pantry.GetByUserIdAsync(userId, ct);
        if (userItems.All(i => i.Id != itemId))
            return DomainErrors.NotFound.PantryItem(itemId);

        await pantry.DeleteAsync(itemId, ct);
        return Result.Success();
    }
}
