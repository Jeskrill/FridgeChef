using FridgeChef.Domain.Common;
using FridgeChef.Domain.Pantry;

namespace FridgeChef.Application.Pantry;

public sealed record PantryItemResponse(Guid Id, long FoodNodeId, decimal? Quantity, long? UnitId, string QuantityMode, DateTime CreatedAt);
public sealed record AddPantryItemRequest(long FoodNodeId, decimal? Quantity, long? UnitId);
public sealed record UpdatePantryItemRequest(decimal? Quantity, long? UnitId);

public sealed class GetPantryItemsHandler
{
    private readonly IPantryRepository _pantry;
    public GetPantryItemsHandler(IPantryRepository pantry) => _pantry = pantry;

    public async Task<IReadOnlyList<PantryItemResponse>> HandleAsync(Guid userId, CancellationToken ct = default)
    {
        var items = await _pantry.GetByUserIdAsync(userId, ct);
        return items.Select(i => new PantryItemResponse(
            i.Id, i.FoodNodeId, i.QuantityValue, i.UnitId, i.QuantityMode.ToString(), i.CreatedAt)).ToList();
    }
}

public sealed class AddPantryItemHandler
{
    private readonly IPantryRepository _pantry;
    public AddPantryItemHandler(IPantryRepository pantry) => _pantry = pantry;

    public async Task<Result<PantryItemResponse>> HandleAsync(
        Guid userId, AddPantryItemRequest request, CancellationToken ct = default)
    {
        if (await _pantry.ExistsAsync(userId, request.FoodNodeId, ct))
            return DomainErrors.Pantry.AlreadyExists;

        var item = new PantryItem
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FoodNodeId = request.FoodNodeId,
            QuantityValue = request.Quantity,
            UnitId = request.UnitId,
            QuantityMode = request.Quantity.HasValue ? QuantityMode.Exact : QuantityMode.Unknown,
            Source = "manual",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _pantry.AddAsync(item, ct);

        return new PantryItemResponse(
            item.Id, item.FoodNodeId, item.QuantityValue, item.UnitId, item.QuantityMode.ToString(), item.CreatedAt);
    }
}

public sealed class UpdatePantryItemHandler
{
    private readonly IPantryRepository _pantry;
    public UpdatePantryItemHandler(IPantryRepository pantry) => _pantry = pantry;

    public async Task<Result<PantryItemResponse>> HandleAsync(
        Guid userId, Guid itemId, UpdatePantryItemRequest request, CancellationToken ct = default)
    {
        var item = await _pantry.GetByIdAsync(itemId, ct);
        if (item is null || item.UserId != userId)
            return DomainErrors.NotFound.PantryItem(itemId);

        if (request.Quantity.HasValue) item.QuantityValue = request.Quantity;
        if (request.UnitId.HasValue) item.UnitId = request.UnitId;
        item.QuantityMode = item.QuantityValue.HasValue ? QuantityMode.Exact : QuantityMode.Unknown;
        item.UpdatedAt = DateTime.UtcNow;

        await _pantry.UpdateAsync(item, ct);

        return new PantryItemResponse(
            item.Id, item.FoodNodeId, item.QuantityValue, item.UnitId, item.QuantityMode.ToString(), item.CreatedAt);
    }
}

public sealed class RemovePantryItemHandler
{
    private readonly IPantryRepository _pantry;
    public RemovePantryItemHandler(IPantryRepository pantry) => _pantry = pantry;

    public async Task<Result> HandleAsync(Guid userId, Guid itemId, CancellationToken ct = default)
    {
        var item = await _pantry.GetByIdAsync(itemId, ct);
        if (item is null || item.UserId != userId)
            return DomainErrors.NotFound.PantryItem(itemId);

        await _pantry.DeleteAsync(itemId, ct);
        return Result.Success();
    }
}
