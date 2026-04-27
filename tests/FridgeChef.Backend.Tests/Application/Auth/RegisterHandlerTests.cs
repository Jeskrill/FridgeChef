using FluentAssertions;
using FridgeChef.Auth.Application.Dto;
using FridgeChef.Auth.Application.UseCases;
using FridgeChef.Auth.Domain;
using NSubstitute;

namespace FridgeChef.Backend.Tests.Application.Auth;

public sealed class RegisterHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldNormalizeEmailBeforeSavingUser()
    {
        var users = Substitute.For<IUserRepository>();
        var hasher = Substitute.For<IPasswordHasher>();
        var jwt = Substitute.For<IJwtTokenService>();
        var refreshTokens = Substitute.For<IRefreshTokenRepository>();
        var handler = new RegisterHandler(users, hasher, jwt, refreshTokens);

        users.EmailExistsAsync("chef@test.com", CancellationToken.None).Returns(false);
        hasher.Hash("password").Returns("hash");
        jwt.GenerateAccessToken(Arg.Any<User>()).Returns("access-token");
        jwt.GenerateRefreshToken().Returns("refresh-token");
        jwt.HashRefreshToken("refresh-token").Returns("refresh-token-hash");

        var result = await handler.HandleAsync(
            new RegisterRequest("  Chef@Test.COM  ", "password", " Chef "),
            new AuthClientContext("tests", null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await users.Received(1).AddAsync(
            Arg.Is<User>(user => user.Email == "chef@test.com" && user.DisplayName == "Chef"),
            CancellationToken.None);
    }
}
