using FluentAssertions;
using FridgeChef.Application.Profile;
using FridgeChef.Domain.Auth;
using NSubstitute;

namespace FridgeChef.Backend.Tests.Application.Profile;

public sealed class ChangePasswordHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldUpdatePasswordAndRevokeRefreshTokensInsideTransaction()
    {
        var users = Substitute.For<IUserRepository>();
        var hasher = Substitute.For<IPasswordHasher>();
        var refreshTokens = Substitute.For<IRefreshTokenRepository>();
        var transactions = new RecordingAuthTransactionManager();
        var handler = new ChangePasswordHandler(users, hasher, refreshTokens, transactions);
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "chef@test.com",
            DisplayName = "Chef",
            PasswordHash = "old-hash",
            Role = "user",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        users.GetByIdAsync(userId, CancellationToken.None).Returns(user);
        hasher.Verify("old-password", "old-hash").Returns(true);
        hasher.Hash("new-password").Returns("new-hash");

        var result = await handler.HandleAsync(
            userId,
            new ChangePasswordRequest("old-password", "new-password"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        transactions.ExecutionCount.Should().Be(1);
        user.PasswordHash.Should().Be("new-hash");
        await users.Received(1).UpdateAsync(user, CancellationToken.None);
        await refreshTokens.Received(1).RevokeAllForUserAsync(userId, CancellationToken.None);
    }

    [Fact]
    public async Task HandleAsync_ShouldStopBeforeTransaction_WhenPasswordIsWrong()
    {
        var users = Substitute.For<IUserRepository>();
        var hasher = Substitute.For<IPasswordHasher>();
        var refreshTokens = Substitute.For<IRefreshTokenRepository>();
        var transactions = new RecordingAuthTransactionManager();
        var handler = new ChangePasswordHandler(users, hasher, refreshTokens, transactions);
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "chef@test.com",
            DisplayName = "Chef",
            PasswordHash = "old-hash",
            Role = "user",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        users.GetByIdAsync(userId, CancellationToken.None).Returns(user);
        hasher.Verify("wrong-password", "old-hash").Returns(false);

        var result = await handler.HandleAsync(
            userId,
            new ChangePasswordRequest("wrong-password", "new-password"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        transactions.ExecutionCount.Should().Be(0);
        await users.DidNotReceive().UpdateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await refreshTokens.DidNotReceive().RevokeAllForUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    private sealed class RecordingAuthTransactionManager : IAuthTransactionManager
    {
        public int ExecutionCount { get; private set; }

        public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct)
        {
            ExecutionCount++;
            return await operation(ct);
        }

        public async Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken ct)
        {
            ExecutionCount++;
            await operation(ct);
        }
    }
}
