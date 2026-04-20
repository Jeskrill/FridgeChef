using FluentAssertions;
using FridgeChef.Pantry.Application.UseCases;
using FridgeChef.Pantry.Domain;
using FridgeChef.SharedKernel;
using NSubstitute;

namespace FridgeChef.Backend.Tests.Application.Pantry;

public sealed class UpdatePantryItemHandlerTests
{
    private static PantryItem MakeItem(Guid id, Guid userId) => new(
        Id: id,
        UserId: userId,
        FoodNodeId: 15,
        QuantityValue: null,
        UnitId: null,
        QuantityMode: QuantityMode.Unknown,
        NormalizedAmountG: null,
        NormalizedAmountMl: null,
        Source: "manual",
        Note: null,
        ExpiresAt: null,
        CreatedAt: DateTime.UtcNow,
        UpdatedAt: DateTime.UtcNow);

    [Fact]
    public async Task HandleAsync_ShouldUpdateItem_WhenUserOwnsIt()
    {
        var repository = Substitute.For<IPantryRepository>();
        var handler    = new UpdatePantryItemHandler(repository);

        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var item   = MakeItem(itemId, userId);

        repository.GetByIdAsync(itemId, CancellationToken.None).Returns(item);

        var result = await handler.HandleAsync(
            userId, itemId,
            new UpdatePantryItemRequest(Quantity: 3m, UnitId: 2),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await repository.Received(1).UpdateAsync(
            Arg.Is<PantryItem>(p => p.QuantityValue == 3m && p.UnitId == 2),
            CancellationToken.None);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_WhenItemBelongsToOtherUser()
    {
        var repository = Substitute.For<IPantryRepository>();
        var handler    = new UpdatePantryItemHandler(repository);

        var userId      = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var itemId      = Guid.NewGuid();
        var item        = MakeItem(itemId, otherUserId); // чужой элемент

        repository.GetByIdAsync(itemId, CancellationToken.None).Returns(item);

        var result = await handler.HandleAsync(
            userId, itemId,
            new UpdatePantryItemRequest(Quantity: 3m, UnitId: null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await repository.DidNotReceive().UpdateAsync(Arg.Any<PantryItem>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_WhenItemDoesNotExist()
    {
        var repository = Substitute.For<IPantryRepository>();
        var handler    = new UpdatePantryItemHandler(repository);

        repository.GetByIdAsync(Arg.Any<Guid>(), CancellationToken.None).Returns((PantryItem?)null);

        var result = await handler.HandleAsync(
            Guid.NewGuid(), Guid.NewGuid(),
            new UpdatePantryItemRequest(Quantity: 1m, UnitId: null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOT_FOUND_PANTRY_ITEM");
    }
}
