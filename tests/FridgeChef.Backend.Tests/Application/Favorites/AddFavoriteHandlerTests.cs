using FluentAssertions;
using FridgeChef.Favorites.Application.UseCases;
using FridgeChef.Favorites.Domain;
using FridgeChef.SharedKernel;
using NSubstitute;

namespace FridgeChef.Backend.Tests.Application.Favorites;

public sealed class AddFavoriteHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldAddFavorite_WhenNotYetAdded()
    {
        var favorites = Substitute.For<IFavoriteRecipeRepository>();
        var handler   = new AddFavoriteHandler(favorites);

        var userId   = Guid.NewGuid();
        var recipeId = Guid.NewGuid();

        favorites.ExistsAsync(userId, recipeId, CancellationToken.None).Returns(false);

        var result = await handler.HandleAsync(userId, recipeId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await favorites.Received(1).AddAsync(
            Arg.Is<FavoriteRecipe>(f => f.UserId == userId && f.RecipeId == recipeId),
            CancellationToken.None);
    }

    [Fact]
    public async Task HandleAsync_ShouldBeIdempotent_WhenAlreadyFavorited()
    {
        var favorites = Substitute.For<IFavoriteRecipeRepository>();
        var handler   = new AddFavoriteHandler(favorites);

        var userId   = Guid.NewGuid();
        var recipeId = Guid.NewGuid();

        favorites.ExistsAsync(userId, recipeId, CancellationToken.None).Returns(true);

        var result = await handler.HandleAsync(userId, recipeId, CancellationToken.None);

        // Уже в избранном — успех, но AddAsync не вызывается
        result.IsSuccess.Should().BeTrue();
        await favorites.DidNotReceive().AddAsync(Arg.Any<FavoriteRecipe>(), Arg.Any<CancellationToken>());
    }
}
