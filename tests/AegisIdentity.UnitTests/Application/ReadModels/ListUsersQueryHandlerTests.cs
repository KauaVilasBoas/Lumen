using AegisIdentity.Domain.Authorization;
using AegisIdentity.Domain.Users;
using AegisIdentity.ReadModels.Queries;
using FluentAssertions;
using NSubstitute;

namespace AegisIdentity.UnitTests.Application.ReadModels;

public sealed class ListUsersQueryHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IProfileRepository _profileRepository = Substitute.For<IProfileRepository>();

    // ──────────────────────────────────────────────────────────────────────────
    // State derivation
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ActiveUser_ReturnsStateActive()
    {
        var user = BuildConfirmedUser();
        SetupRepositories([user]);

        var result = await InvokeHandler(ListUsersQueryHandler.UserStateFilter.All);

        result.Items.Should().HaveCount(1);
        result.Items[0].State.Should().Be("active");
    }

    [Fact]
    public async Task Handle_PendingUser_EmailNotConfirmed_ReturnsStatePending()
    {
        var user = BuildUser(emailConfirmedAt: null);
        SetupRepositories([user]);

        var result = await InvokeHandler(ListUsersQueryHandler.UserStateFilter.All);

        result.Items[0].State.Should().Be("pending");
    }

    [Fact]
    public async Task Handle_LockedUser_LockedUntilInFuture_ReturnsStateLocked()
    {
        var user = BuildLockedUser(lockedUntil: DateTime.UtcNow.AddHours(1));
        SetupRepositories([user]);

        var result = await InvokeHandler(ListUsersQueryHandler.UserStateFilter.All);

        result.Items[0].State.Should().Be("locked");
    }

    [Fact]
    public async Task Handle_DeletedUser_ReturnsStateDeleted()
    {
        var user = BuildDeletedUser();
        SetupRepositories([user], includeDeleted: true);

        var result = await InvokeHandler(ListUsersQueryHandler.UserStateFilter.All);

        result.Items[0].State.Should().Be("deleted");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // State filter (in-memory narrowing)
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_FilterByActive_ExcludesOtherStates()
    {
        var activeUser = BuildConfirmedUser();
        var pendingUser = BuildUser(emailConfirmedAt: null);
        SetupRepositories([activeUser, pendingUser]);

        var result = await InvokeHandler(ListUsersQueryHandler.UserStateFilter.Active);

        result.Items.Should().HaveCount(1);
        result.Items[0].Id.Should().Be(activeUser.Id);
    }

    [Fact]
    public async Task Handle_FilterByPending_ExcludesOtherStates()
    {
        var activeUser = BuildConfirmedUser();
        var pendingUser = BuildUser(emailConfirmedAt: null);
        SetupRepositories([activeUser, pendingUser]);

        var result = await InvokeHandler(ListUsersQueryHandler.UserStateFilter.Pending);

        result.Items.Should().HaveCount(1);
        result.Items[0].Id.Should().Be(pendingUser.Id);
    }

    [Fact]
    public async Task Handle_FilterByLocked_ExcludesOtherStates()
    {
        var lockedUser = BuildLockedUser(DateTime.UtcNow.AddHours(1));
        var activeUser = BuildConfirmedUser();
        SetupRepositories([lockedUser, activeUser]);

        var result = await InvokeHandler(ListUsersQueryHandler.UserStateFilter.Locked);

        result.Items.Should().HaveCount(1);
        result.Items[0].Id.Should().Be(lockedUser.Id);
    }

    [Fact]
    public async Task Handle_FilterByAll_IncludesDeletedRows_PassesIncludeDeletedTrueToRepository()
    {
        var user = BuildConfirmedUser();
        SetupRepositories([user], includeDeleted: true);

        await InvokeHandler(ListUsersQueryHandler.UserStateFilter.All);

        await _userRepository.Received(1).ListAsync(
            Arg.Any<string?>(),
            includeDeleted: true,
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_FilterByDeleted_PassesIncludeDeletedTrueToRepository()
    {
        var user = BuildDeletedUser();
        SetupRepositories([user], includeDeleted: true);

        await InvokeHandler(ListUsersQueryHandler.UserStateFilter.Deleted);

        await _userRepository.Received(1).ListAsync(
            Arg.Any<string?>(),
            includeDeleted: true,
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_FilterByActive_PassesIncludeDeletedFalseToRepository()
    {
        SetupRepositories([]);

        await InvokeHandler(ListUsersQueryHandler.UserStateFilter.Active);

        await _userRepository.Received(1).ListAsync(
            Arg.Any<string?>(),
            includeDeleted: false,
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Profile and permission counts
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_UserWithTwoProfiles_ReturnsProfileCountTwo()
    {
        var user = BuildConfirmedUser();
        var profileA = Profile.Create("Admin", "Full access");
        var profileB = Profile.Create("Viewer", "Read-only");

        SetupRepositories([user]);
        _profileRepository.GetProfilesByUserIdAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(new[] { profileA, profileB });
        _profileRepository.GetPermissionCodesByUserIdAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(new HashSet<string> { "Profiles.List", "Profiles.Get" });

        var result = await InvokeHandler(ListUsersQueryHandler.UserStateFilter.All);

        result.Items[0].ProfileCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_UserWithDistinctPermissions_ReturnsResolvedPermissionCount()
    {
        var user = BuildConfirmedUser();
        var profile = Profile.Create("Admin", "Full access");

        SetupRepositories([user]);
        _profileRepository.GetProfilesByUserIdAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(new[] { profile });
        _profileRepository.GetPermissionCodesByUserIdAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(new HashSet<string> { "Users.List", "Users.Get", "Profiles.List" });

        var result = await InvokeHandler(ListUsersQueryHandler.UserStateFilter.All);

        result.Items[0].ResolvedPermissionCount.Should().Be(3);
    }

    [Fact]
    public async Task Handle_UserWithNoProfiles_ReturnsZeroProfileAndPermissionCounts()
    {
        var user = BuildConfirmedUser();
        SetupRepositories([user]);
        _profileRepository.GetProfilesByUserIdAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Profile>());

        var result = await InvokeHandler(ListUsersQueryHandler.UserStateFilter.All);

        result.Items[0].ProfileCount.Should().Be(0);
        result.Items[0].ResolvedPermissionCount.Should().Be(0);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Paging and empty results
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_EmptyPage_ReturnsEmptyItemsWithZeroTotal()
    {
        _userRepository.ListAsync(
                Arg.Any<string?>(),
                Arg.Any<bool>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<User>)[], 0));

        var result = await InvokeHandler(ListUsersQueryHandler.UserStateFilter.All);

        result.Items.Should().BeEmpty();
        result.Total.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ForwardsSearchAndPagingToRepository()
    {
        SetupRepositories([]);

        var query = new ListUsersQueryHandler.Query(
            Search: "alice",
            State: ListUsersQueryHandler.UserStateFilter.Active,
            Page: 2,
            PageSize: 10);

        await CreateHandler().Handle(query, CancellationToken.None);

        await _userRepository.Received(1).ListAsync(
            search: "alice",
            includeDeleted: false,
            page: 2,
            pageSize: 10,
            ct: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PagedResult_ReturnsCorrectMetadata()
    {
        var user = BuildConfirmedUser();
        _userRepository.ListAsync(
                Arg.Any<string?>(),
                Arg.Any<bool>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<User>)[user], 42));
        SetupProfileRepositoryDefaults(user.Id);

        var query = new ListUsersQueryHandler.Query(
            Search: null,
            State: ListUsersQueryHandler.UserStateFilter.All,
            Page: 3,
            PageSize: 5);

        var result = await CreateHandler().Handle(query, CancellationToken.None);

        result.Page.Should().Be(3);
        result.PageSize.Should().Be(5);
        result.Total.Should().Be(42);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Scalar field mapping
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_MapsAllScalarFieldsCorrectly()
    {
        var lockoutEnd = DateTime.UtcNow.AddHours(2);
        var user = BuildLockedUser(lockoutEnd);
        SetupRepositories([user]);
        _profileRepository.GetProfilesByUserIdAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Profile>());

        var result = await InvokeHandler(ListUsersQueryHandler.UserStateFilter.All);

        var item = result.Items[0];
        item.Id.Should().Be(user.Id);
        item.Username.Should().Be(user.Username);
        item.Email.Should().Be(user.Email);
        item.CreatedAt.Should().Be(user.CreatedAt);
        item.LockoutEndAt.Should().BeCloseTo(lockoutEnd, precision: TimeSpan.FromSeconds(1));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────────

    private ListUsersQueryHandler CreateHandler() =>
        new(_userRepository, _profileRepository);

    private async Task<ListUsersQueryHandler.PagedResult> InvokeHandler(
        ListUsersQueryHandler.UserStateFilter state,
        string? search = null,
        int page = 1,
        int pageSize = 20)
    {
        var query = new ListUsersQueryHandler.Query(search, state, page, pageSize);
        return await CreateHandler().Handle(query, CancellationToken.None);
    }

    /// <summary>
    /// Configures the user repository to return the given list with a total
    /// equal to the list count, and sets up profile/permission defaults for each user.
    /// </summary>
    private void SetupRepositories(IReadOnlyList<User> users, bool includeDeleted = false)
    {
        _userRepository.ListAsync(
                Arg.Any<string?>(),
                Arg.Any<bool>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns((users, users.Count));

        foreach (var user in users)
            SetupProfileRepositoryDefaults(user.Id);
    }

    private void SetupProfileRepositoryDefaults(Guid userId)
    {
        _profileRepository.GetProfilesByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Profile>());
        _profileRepository.GetPermissionCodesByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new HashSet<string>());
    }

    private static User BuildUser(DateTime? emailConfirmedAt = null)
    {
        var user = User.Create(
            email: $"{Guid.NewGuid():N}@example.com",
            username: $"user-{Guid.NewGuid():N}",
            passwordHash: "hashed");

        user.EmailConfirmedAt = emailConfirmedAt;
        return user;
    }

    private static User BuildConfirmedUser()
        => BuildUser(emailConfirmedAt: DateTime.UtcNow.AddDays(-1));

    private static User BuildLockedUser(DateTime lockedUntil)
    {
        // Use reflection to set the private LockedUntil property for testing purposes.
        // The domain only exposes RecordFailedLogin which requires threshold/duration,
        // making it harder to set an exact future date from a test.
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
