using FluentAssertions;
using FridgeChef.Application.Favorites;
using FridgeChef.Domain.Catalog;
using FridgeChef.Domain.Common;
using FridgeChef.Domain.Favorites;
using NSubstitute;

namespace FridgeChef.Backend.Tests.Application.Favorites;

public sealed class AddFavoriteHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_WhenRecipeDoesNotExist()
    {
        var favorites = Substitute.For<IFavoriteRecipeRepository>();
        var recipes = Substitute.For<IRecipeRepository>();
        var handler = new AddFavoriteHandler(favorites, recipes);
        var userId = Guid.NewGuid();
        var recipeId = Guid.NewGuid();

        recipes.GetByIdAsync(recipeId, CancellationToken.None).Returns((Recipe?)null);

        var result = await handler.HandleAsync(userId, recipeId, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.NotFound.Recipe(recipeId));
        await favorites.DidNotReceive().AddAsync(Arg.Any<FavoriteRecipe>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_WhenRecipeIsNotPublished()
    {
        var favorites = Substitute.For<IFavoriteRecipeRepository>();
        var recipes = Substitute.For<IRecipeRepository>();
        var handler = new AddFavoriteHandler(favorites, recipes);
        var userId = Guid.NewGuid();
        var recipeId = Guid.NewGuid();

        recipes.GetByIdAsync(recipeId, CancellationToken.None).Returns(new Recipe
        {
            Id = recipeId,
            Status = RecipeStatus.Hidden
        });

        var result = await handler.HandleAsync(userId, recipeId, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.NotFound.Recipe(recipeId));
        await favorites.DidNotReceive().AddAsync(Arg.Any<FavoriteRecipe>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ShouldPersistFavorite_WhenRecipeIsPublished()
    {
        var favorites = Substitute.For<IFavoriteRecipeRepository>();
        var recipes = Substitute.For<IRecipeRepository>();
        var handler = new AddFavoriteHandler(favorites, recipes);
        var userId = Guid.NewGuid();
        var recipeId = Guid.NewGuid();

        recipes.GetByIdAsync(recipeId, CancellationToken.None).Returns(new Recipe
        {
            Id = recipeId,
            Status = RecipeStatus.Published
        });

        var result = await handler.HandleAsync(userId, recipeId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await favorites.Received(1).AddAsync(
            Arg.Is<FavoriteRecipe>(favorite =>
                favorite.UserId == userId &&
                favorite.RecipeId == recipeId),
            CancellationToken.None);
    }
}
