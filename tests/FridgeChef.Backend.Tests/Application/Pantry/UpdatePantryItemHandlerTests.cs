using FluentAssertions;
using FridgeChef.Application.Pantry;
using FridgeChef.Domain.Common;
using FridgeChef.Domain.Pantry;
using NSubstitute;

namespace FridgeChef.Backend.Tests.Application.Pantry;

public sealed class UpdatePantryItemHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldRejectStateWithUnitAndNoQuantity()
    {
        var repository = Substitute.For<IPantryRepository>();
        var handler = new UpdatePantryItemHandler(repository);
        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        repository.GetByIdAsync(itemId, CancellationToken.None).Returns(new PantryItem
        {
            Id = itemId,
            UserId = userId,
            FoodNodeId = 15,
            QuantityValue = null,
            UnitId = 2,
            QuantityMode = QuantityMode.Unknown,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        var result = await handler.HandleAsync(
            userId,
            itemId,
            new UpdatePantryItemRequest(null, 3),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Pantry.UnitRequiresQuantity);
        await repository.DidNotReceive().UpdateAsync(Arg.Any<PantryItem>(), Arg.Any<CancellationToken>());
    }
}
