using FluentAssertions;
using FridgeChef.Favorites.Application.UseCases;
using NSubstitute;

namespace FridgeChef.Backend.Tests.Application.Favorites;

public sealed class AddFavoriteHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldAddFavorite_WhenNotYetAdded()
    {
        var favorites = Substitute.For<IFavoriteRecipeRepository>();
        var recipes = Substitute.For<IRecipeSummaryProvider>();
        var handler = new AddFavoriteHandler(favorites, recipes);

        var userId = Guid.NewGuid();
        var recipeId = Guid.NewGuid();

        recipes.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), CancellationToken.None)
            .Returns([new RecipeSummaryDto(recipeId, "borscht", "Borscht", null)]);
        favorites.ExistsAsync(userId, recipeId, CancellationToken.None).Returns(false);

        var result = await handler.HandleAsync(userId, recipeId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await favorites.Received(1).AddAsync(userId, recipeId, CancellationToken.None);
    }

    [Fact]
    public async Task HandleAsync_ShouldBeIdempotent_WhenAlreadyFavorited()
    {
        var favorites = Substitute.For<IFavoriteRecipeRepository>();
        var recipes = Substitute.For<IRecipeSummaryProvider>();
        var handler = new AddFavoriteHandler(favorites, recipes);

        var userId = Guid.NewGuid();
        var recipeId = Guid.NewGuid();

        recipes.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), CancellationToken.None)
            .Returns([new RecipeSummaryDto(recipeId, "borscht", "Borscht", null)]);
        favorites.ExistsAsync(userId, recipeId, CancellationToken.None).Returns(true);

        var result = await handler.HandleAsync(userId, recipeId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await favorites.DidNotReceive().AddAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_WhenRecipeDoesNotExist()
    {
        var favorites = Substitute.For<IFavoriteRecipeRepository>();
        var recipes = Substitute.For<IRecipeSummaryProvider>();
        var handler = new AddFavoriteHandler(favorites, recipes);

        var userId = Guid.NewGuid();
        var recipeId = Guid.NewGuid();

        recipes.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), CancellationToken.None)
            .Returns([]);

        var result = await handler.HandleAsync(userId, recipeId, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOT_FOUND_RECIPE");
        await favorites.DidNotReceive().AddAsync(
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }
}
