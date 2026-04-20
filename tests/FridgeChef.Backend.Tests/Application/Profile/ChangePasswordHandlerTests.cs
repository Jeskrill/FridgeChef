using FluentAssertions;
using FridgeChef.Auth.Application.UseCases;
using FridgeChef.Auth.Domain;
using FridgeChef.SharedKernel;
using NSubstitute;

namespace FridgeChef.Backend.Tests.Application.Profile;

public sealed class ChangePasswordHandlerTests
{
    private static User MakeUser(Guid id, string hash) => new(
        Id: id,
        Email: "chef@test.com",
        PasswordHash: hash,
        DisplayName: "Chef",
        AvatarUrl: null,
        Role: "User",
        IsBlocked: false,
        LastLoginAt: null,
        CreatedAt: DateTime.UtcNow,
        UpdatedAt: DateTime.UtcNow);

    [Fact]
    public async Task HandleAsync_ShouldUpdatePassword_WhenOldPasswordIsCorrect()
    {
        var users  = Substitute.For<IUserRepository>();
        var hasher = Substitute.For<IPasswordHasher>();
        var handler = new ChangePasswordHandler(users, hasher);

        var userId = Guid.NewGuid();
        var user   = MakeUser(userId, "old-hash");

        users.GetByIdAsync(userId, CancellationToken.None).Returns(user);
        hasher.Verify("old-password", "old-hash").Returns(true);
        hasher.Hash("new-password").Returns("new-hash");

        var result = await handler.HandleAsync(
            userId,
            new ChangePasswordRequest("old-password", "new-password"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await users.Received(1).UpdateAsync(
            Arg.Is<User>(u => u.PasswordHash == "new-hash"),
            CancellationToken.None);
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenOldPasswordIsWrong()
    {
        var users  = Substitute.For<IUserRepository>();
        var hasher = Substitute.For<IPasswordHasher>();
        var handler = new ChangePasswordHandler(users, hasher);

        var userId = Guid.NewGuid();
        var user   = MakeUser(userId, "old-hash");

        users.GetByIdAsync(userId, CancellationToken.None).Returns(user);
        hasher.Verify("wrong-password", "old-hash").Returns(false);

        var result = await handler.HandleAsync(
            userId,
            new ChangePasswordRequest("wrong-password", "new-password"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Auth.InvalidCredentials);
        await users.DidNotReceive().UpdateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }
}
