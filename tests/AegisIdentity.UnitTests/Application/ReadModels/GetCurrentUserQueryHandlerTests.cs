using AegisIdentity.Domain.Users;
using AegisIdentity.ReadModels.Queries;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

namespace AegisIdentity.UnitTests.Application.ReadModels;

public sealed class GetCurrentUserQueryHandlerTests
{
    private const string UserId = "aabbccddeeff00112233aabb";

    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();

    [Fact]
    public async Task Handle_WhenUserFoundWithAllFieldsPopulated_ReturnsMappedResult()
    {
        var user = BuildUser(
            lastLoginAt: new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc),
            emailConfirmedAt: new DateTime(2026, 1, 10, 8, 0, 0, DateTimeKind.Utc));

        _userRepository.FindByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(user);

        var result = await CreateHandler().Handle(new GetCurrentUserQueryHandler.Query(UserId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
        result.Email.Should().Be(user.Email);
        result.Username.Should().Be(user.Username);
        result.Roles.Should().BeEquivalentTo(user.Roles);
        result.CreatedAt.Should().Be(user.CreatedAt);
        result.LastLoginAt.Should().Be(user.LastLoginAt);
        result.EmailConfirmedAt.Should().Be(user.EmailConfirmedAt);
    }

    [Fact]
    public async Task Handle_WhenUserFoundWithNullableFieldsAbsent_ReturnsResultWithNullFields()
    {
        var user = BuildUser(lastLoginAt: null, emailConfirmedAt: null);

        _userRepository.FindByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(user);

        var result = await CreateHandler().Handle(new GetCurrentUserQueryHandler.Query(UserId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.LastLoginAt.Should().BeNull();
        result.EmailConfirmedAt.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsNull()
    {
        _userRepository.FindByIdAsync(UserId, Arg.Any<CancellationToken>()).ReturnsNull();

        var result = await CreateHandler().Handle(new GetCurrentUserQueryHandler.Query(UserId), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public void Result_DoesNotExposePasswordHashOrSensitiveFields()
    {
        var resultType = typeof(GetCurrentUserQueryHandler.Result);

        resultType.GetProperty("PasswordHash").Should().BeNull();
        resultType.GetProperty("FailedLoginAttempts").Should().BeNull();
        resultType.GetProperty("LockedUntil").Should().BeNull();
    }

    private GetCurrentUserQueryHandler CreateHandler() =>
        new(_userRepository);

    private static User BuildUser(DateTime? lastLoginAt, DateTime? emailConfirmedAt)
    {
        var user = User.Create(
            email: "test@example.com",
            username: "testuser",
            passwordHash: "hashed-password");

        user.LastLoginAt = lastLoginAt;
        user.EmailConfirmedAt = emailConfirmedAt;

        return user;
    }
}
