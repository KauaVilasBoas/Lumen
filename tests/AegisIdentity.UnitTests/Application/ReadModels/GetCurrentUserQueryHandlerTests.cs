using AegisIdentity.Domain.Authorization;
using AegisIdentity.Domain.Users;
using AegisIdentity.ReadModels.Queries;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

namespace AegisIdentity.UnitTests.Application.ReadModels;

public sealed class GetCurrentUserQueryHandlerTests
{
    private static readonly Guid UserId = Guid.Parse("aabbccdd-eeff-0011-2233-aabbccddeeff");

    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IProfileRepository _profileRepository = Substitute.For<IProfileRepository>();

    [Fact]
    public async Task Handle_WhenUserHasNoProfiles_ReturnsEmptyProfilesList()
    {
        _userRepository.FindByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(BuildUser());
        _profileRepository.GetProfilesByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Profile>());

        var result = await CreateHandler().Handle(new GetCurrentUserQueryHandler.Query(UserId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Profiles.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenUserHasOneProfile_ReturnsSingleProfileWithIdAndName()
    {
        var profile = Profile.Create("Administrator", "Full access profile");

        _userRepository.FindByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(BuildUser());
        _profileRepository.GetProfilesByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(new[] { profile });

        var result = await CreateHandler().Handle(new GetCurrentUserQueryHandler.Query(UserId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Profiles.Should().HaveCount(1);
        result.Profiles[0].Id.Should().Be(profile.Id);
        result.Profiles[0].Name.Should().Be("Administrator");
    }

    [Fact]
    public async Task Handle_WhenUserHasMultipleProfiles_ReturnsAllWithCorrectIdAndName()
    {
        var adminProfile = Profile.Create("Administrator", "Full access profile");
        var userProfile = Profile.Create("User", "Basic access profile");

        _userRepository.FindByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(BuildUser());
        _profileRepository.GetProfilesByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(new[] { adminProfile, userProfile });

        var result = await CreateHandler().Handle(new GetCurrentUserQueryHandler.Query(UserId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Profiles.Should().HaveCount(2);
        result.Profiles.Should().ContainSingle(p => p.Id == adminProfile.Id && p.Name == "Administrator");
        result.Profiles.Should().ContainSingle(p => p.Id == userProfile.Id && p.Name == "User");
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsNull()
    {
        _userRepository.FindByIdAsync(UserId, Arg.Any<CancellationToken>()).ReturnsNull();

        var result = await CreateHandler().Handle(new GetCurrentUserQueryHandler.Query(UserId), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenUserFoundWithAllFieldsPopulated_ReturnsMappedScalarFields()
    {
        var user = BuildUser(
            lastLoginAt: new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc),
            emailConfirmedAt: new DateTime(2026, 1, 10, 8, 0, 0, DateTimeKind.Utc));

        _userRepository.FindByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(user);
        _profileRepository.GetProfilesByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Profile>());

        var result = await CreateHandler().Handle(new GetCurrentUserQueryHandler.Query(UserId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id.ToString());
        result.Email.Should().Be(user.Email);
        result.Username.Should().Be(user.Username);
        result.CreatedAt.Should().Be(user.CreatedAt);
        result.LastLoginAt.Should().Be(user.LastLoginAt);
        result.EmailConfirmedAt.Should().Be(user.EmailConfirmedAt);
    }

    [Fact]
    public async Task Handle_WhenUserFoundWithNullableFieldsAbsent_ReturnsResultWithNullFields()
    {
        var user = BuildUser(lastLoginAt: null, emailConfirmedAt: null);

        _userRepository.FindByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(user);
        _profileRepository.GetProfilesByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Profile>());

        var result = await CreateHandler().Handle(new GetCurrentUserQueryHandler.Query(UserId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.LastLoginAt.Should().BeNull();
        result.EmailConfirmedAt.Should().BeNull();
    }

    [Fact]
    public void Result_DoesNotExposePasswordHashOrSensitiveFields()
    {
        var resultType = typeof(GetCurrentUserQueryHandler.Result);

        resultType.GetProperty("PasswordHash").Should().BeNull();
        resultType.GetProperty("FailedLoginAttempts").Should().BeNull();
        resultType.GetProperty("LockedUntil").Should().BeNull();
    }

    [Fact]
    public async Task Handle_DoesNotCallListAllAsync_UsesSingleFilteredQuery()
    {
        _userRepository.FindByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(BuildUser());
        _profileRepository.GetProfilesByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Profile>());

        await CreateHandler().Handle(new GetCurrentUserQueryHandler.Query(UserId), CancellationToken.None);

        await _profileRepository.Received(1).GetProfilesByUserIdAsync(UserId, Arg.Any<CancellationToken>());
        await _profileRepository.DidNotReceive().ListAllAsync(Arg.Any<CancellationToken>());
    }

    private GetCurrentUserQueryHandler CreateHandler() =>
        new(_userRepository, _profileRepository);

    private static User BuildUser(DateTime? lastLoginAt = null, DateTime? emailConfirmedAt = null)
    {
        var user = User.Create(
            email: "test@example.com",
            username: "testuser",
            passwordHash: "hashed-password");

        SetUserProperty(user, nameof(User.LastLoginAt), lastLoginAt);
        SetUserProperty(user, nameof(User.EmailConfirmedAt), emailConfirmedAt);

        return user;
    }

    private static void SetUserProperty(User user, string propertyName, object? value)
        => typeof(User).GetProperty(propertyName)!.SetValue(user, value);
}
