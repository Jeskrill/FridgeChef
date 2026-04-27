using FluentAssertions;
using FridgeChef.Pantry.Application.UseCases;
using NSubstitute;

namespace FridgeChef.Backend.Tests.Application.Pantry;

public sealed class UpdatePantryItemHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldUpdateItem_WhenUserOwnsIt()
    {
        var repository = Substitute.For<IPantryRepository>();
        var handler = new UpdatePantryItemHandler(repository);

        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var request = new UpdatePantryItemRequest(Quantity: 3m, UnitId: 2);
        var expectedResponse = new PantryItemResponse(itemId, 15, 3m, 2, FridgeChef.Pantry.Domain.QuantityMode.Exact, DateTime.UtcNow);

        repository.ExistsByIdAndUserAsync(itemId, userId, CancellationToken.None).Returns(true);
        repository.UpdateAsync(itemId, request, CancellationToken.None).Returns(expectedResponse);

        var result = await handler.HandleAsync(userId, itemId, request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Quantity.Should().Be(3m);
        result.Value.UnitId.Should().Be(2);
        await repository.Received(1).UpdateAsync(itemId, request, CancellationToken.None);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_WhenItemBelongsToOtherUser()
    {
        var repository = Substitute.For<IPantryRepository>();
        var handler = new UpdatePantryItemHandler(repository);

        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        repository.ExistsByIdAndUserAsync(itemId, userId, CancellationToken.None).Returns(false);

        var result = await handler.HandleAsync(
            userId, itemId,
            new UpdatePantryItemRequest(Quantity: 3m, UnitId: null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await repository.DidNotReceive().UpdateAsync(
            Arg.Any<Guid>(), Arg.Any<UpdatePantryItemRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_WhenItemDoesNotExist()
    {
        var repository = Substitute.For<IPantryRepository>();
        var handler = new UpdatePantryItemHandler(repository);

        var itemId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        repository.ExistsByIdAndUserAsync(itemId, userId, CancellationToken.None).Returns(false);

        var result = await handler.HandleAsync(
            userId, itemId,
            new UpdatePantryItemRequest(Quantity: 1m, UnitId: null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOT_FOUND_PANTRY_ITEM");
    }
}
