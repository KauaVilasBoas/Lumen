using AegisIdentity.Domain.Authorization;
using AegisIdentity.Domain.Users;
using AegisIdentity.ReadModels.Queries;
using AegisIdentity.SharedKernel.Exceptions;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

namespace AegisIdentity.UnitTests.Application.ReadModels;

public sealed class GetUserDetailQueryHandlerTests
{
    private static readonly Guid UserId = Guid.Parse("aabbccdd-eeff-0011-2233-aabbccddeeff");

    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IProfileRepository _profileRepository = Substitute.For<IProfileRepository>();

    // ──────────────────────────────────────────────────────────────────────────
    // Not found
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_UserNotFound_ThrowsNotFoundException()
    {
        _userRepository.FindByIdIgnoringFiltersAsync(UserId, Arg.Any<CancellationToken>()).ReturnsNull();

        var act = () => InvokeHandler(UserId);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // State derivation
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ActiveUser_ReturnsStateActive()
    {
        var user = BuildConfirmedUser();
        SetupUser(user);

        var result = await InvokeHandler(UserId);

        result.State.Should().Be("active");
    }

    [Fact]
    public async Task Handle_PendingUser_EmailNotConfirmed_ReturnsStatePending()
    {
        var user = BuildUser(emailConfirmedAt: null);
        SetupUser(user);

        var result = await InvokeHandler(UserId);

        result.State.Should().Be("pending");
    }

    [Fact]
    public async Task Handle_LockedUser_LockedUntilInFuture_ReturnsStateLocked()
    {
        var user = BuildLockedUser(DateTime.UtcNow.AddHours(1));
        SetupUser(user);

        var result = await InvokeHandler(UserId);

        result.State.Should().Be("locked");
    }

    [Fact]
    public async Task Handle_DeletedUser_ReturnsStateDeleted()
    {
        var user = BuildDeletedUser();
        SetupUser(user);

        var result = await InvokeHandler(UserId);

        result.State.Should().Be("deleted");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Soft-deleted user is accessible
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_SoftDeletedUser_ReturnsResultWithDeletedState()
    {
        var user = BuildDeletedUser();
        SetupUser(user);

        var result = await InvokeHandler(UserId);

        result.Should().NotBeNull();
        result.State.Should().Be("deleted");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Scalar field mapping
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_MapsAllScalarFieldsCorrectly()
    {
        var lockoutEnd = DateTime.UtcNow.AddHours(2);
        var user = BuildLockedUser(lockoutEnd);
        SetupUser(user);

        var result = await InvokeHandler(UserId);

        result.Id.Should().Be(user.Id);
        result.Username.Should().Be(user.Username);
        result.Email.Should().Be(user.Email);
        result.CreatedAt.Should().Be(user.CreatedAt);
        result.LockoutEndAt.Should().BeCloseTo(lockoutEnd, precision: TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Handle_NullableFieldsAbsent_ReturnsNullForThoseFields()
    {
        var user = BuildUser(emailConfirmedAt: null, lastLoginAt: null);
        SetupUser(user);

        var result = await InvokeHandler(UserId);

        result.EmailConfirmedAt.Should().BeNull();
        result.LastLoginAt.Should().BeNull();
        result.LockoutEndAt.Should().BeNull();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Profiles
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_UserWithNoProfiles_ReturnsEmptyProfilesList()
    {
        var user = BuildConfirmedUser();
        SetupUser(user);

        var result = await InvokeHandler(UserId);

        result.Profiles.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_UserWithOneProfile_ReturnsSingleProfileSummary()
    {
        var user = BuildConfirmedUser();
        var profile = Profile.Create("Admin", "Full access");

        SetupUser(user);
        _profileRepository.GetProfilesByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(new[] { profile });
        _profileRepository.GetPermissionCountsByProfileIdsAsync(
                Arg.Is<IReadOnlyList<Guid>>(ids => ids.Contains(profile.Id)),
                Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, int> { [profile.Id] = 1 });

        var result = await InvokeHandler(UserId);

        result.Profiles.Should().HaveCount(1);
        result.Profiles[0].ProfileId.Should().Be(profile.Id);
        result.Profiles[0].Name.Should().Be("Admin");
        result.Profiles[0].IsSystem.Should().BeFalse();
        result.Profiles[0].PermissionCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_UserWithSystemProfile_ExposesIsSystemTrue()
    {
        var user = BuildConfirmedUser();
        var systemProfile = Profile.Create("System", "System profile", isSystem: true);

        SetupUser(user);
        _profileRepository.GetProfilesByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(new[] { systemProfile });
        _profileRepository.GetPermissionCountsByProfileIdsAsync(
                Arg.Any<IReadOnlyList<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, int>());

        var result = await InvokeHandler(UserId);

        result.Profiles[0].IsSystem.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ProfileWithTwoPermissions_ReturnsPermissionCountTwo()
    {
        var user = BuildConfirmedUser();
        var profile = Profile.Create("Operator", "Operator access");

        SetupUser(user);
        _profileRepository.GetProfilesByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(new[] { profile });
        _profileRepository.GetPermissionCountsByProfileIdsAsync(
                Arg.Any<IReadOnlyList<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, int> { [profile.Id] = 2 });

        var result = await InvokeHandler(UserId);

        result.Profiles[0].PermissionCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_BatchPermissionCountCalled_NotPerProfileLoop()
    {
        var user = BuildConfirmedUser();
        var profileA = Profile.Create("Admin", "Full access");
        var profileB = Profile.Create("Viewer", "Read-only");

        SetupUser(user);
        _profileRepository.GetProfilesByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(new[] { profileA, profileB });
        _profileRepository.GetPermissionCountsByProfileIdsAsync(
                Arg.Any<IReadOnlyList<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, int> { [profileA.Id] = 3, [profileB.Id] = 1 });

        var result = await InvokeHandler(UserId);

        await _profileRepository.Received(1)
            .GetPermissionCountsByProfileIdsAsync(Arg.Any<IReadOnlyList<Guid>>(), Arg.Any<CancellationToken>());

        result.Profiles.Should().HaveCount(2);
        result.Profiles.First(p => p.ProfileId == profileA.Id).PermissionCount.Should().Be(3);
        result.Profiles.First(p => p.ProfileId == profileB.Id).PermissionCount.Should().Be(1);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Resolved permission count
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_UserWithThreeDistinctPermissions_ReturnsResolvedPermissionCountThree()
    {
        var user = BuildConfirmedUser();
        SetupUser(user);
        _profileRepository.GetPermissionCodesByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(new HashSet<string> { "Users.List", "Users.Get", "Profiles.List" });

        var result = await InvokeHandler(UserId);

        result.ResolvedPermissionCount.Should().Be(3);
    }

    [Fact]
    public async Task Handle_UserWithNoPermissions_ReturnsResolvedPermissionCountZero()
    {
        var user = BuildConfirmedUser();
        SetupUser(user);

        var result = await InvokeHandler(UserId);

        result.ResolvedPermissionCount.Should().Be(0);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Repository access patterns
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_UsesIgnoringFiltersOverload_NotStandardFindById()
    {
        var user = BuildConfirmedUser();
        SetupUser(user);

        await InvokeHandler(UserId);

        await _userRepository.Received(1).FindByIdIgnoringFiltersAsync(UserId, Arg.Any<CancellationToken>());
        await _userRepository.DidNotReceive().FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────────

    private GetUserDetailQueryHandler CreateHandler() =>
        new(_userRepository, _profileRepository);

    private Task<GetUserDetailQueryHandler.Result> InvokeHandler(Guid userId)
        => CreateHandler().Handle(new GetUserDetailQueryHandler.Query(userId), CancellationToken.None);

    private void SetupUser(User user)
    {
        _userRepository.FindByIdIgnoringFiltersAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(user);

        _profileRepository.GetProfilesByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Profile>());

        _profileRepository.GetPermissionCodesByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(new HashSet<string>());

        _profileRepository.GetPermissionCountsByProfileIdsAsync(
                Arg.Any<IReadOnlyList<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, int>());
    }

    private static User BuildUser(
        DateTime? emailConfirmedAt = null,
        DateTime? lastLoginAt = null)
    {
        var user = User.Create(
            email: $"{Guid.NewGuid():N}@example.com",
            username: $"user-{Guid.NewGuid():N}",
            passwordHash: "hashed");

        user.EmailConfirmedAt = emailConfirmedAt;
        user.LastLoginAt = lastLoginAt;
        return user;
    }

    private static User BuildConfirmedUser()
        => BuildUser(emailConfirmedAt: DateTime.UtcNow.AddDays(-1));

    private static User BuildLockedUser(DateTime lockedUntil)
    {
        var user = BuildConfirmedUser();
        typeof(User)
            .GetProperty(nameof(user.LockedUntil))!
            .SetValue(user, lockedUntil);
        return user;
    }

    private static User BuildDeletedUser()
    {
        var user = BuildConfirmedUser();
        user.SoftDelete();
        return user;
    }
}
