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

        var result = await InvokeHandler("all");

        result.Items.Should().HaveCount(1);
        result.Items[0].State.Should().Be("active");
    }

    [Fact]
    public async Task Handle_PendingUser_EmailNotConfirmed_ReturnsStatePending()
    {
        var user = BuildUser(emailConfirmedAt: null);
        SetupRepositories([user]);

        var result = await InvokeHandler("all");

        result.Items[0].State.Should().Be("pending");
    }

    [Fact]
    public async Task Handle_LockedUser_LockedUntilInFuture_ReturnsStateLocked()
    {
        var user = BuildLockedUser(lockedUntil: DateTime.UtcNow.AddHours(1));
        SetupRepositories([user]);

        var result = await InvokeHandler("all");

        result.Items[0].State.Should().Be("locked");
    }

    [Fact]
    public async Task Handle_DeletedUser_ReturnsStateDeleted()
    {
        var user = BuildDeletedUser();
        SetupRepositories([user], includeDeleted: true);

        var result = await InvokeHandler("all");

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

        var result = await InvokeHandler("active");

        result.Items.Should().HaveCount(1);
        result.Items[0].Id.Should().Be(activeUser.Id);
    }

    [Fact]
    public async Task Handle_FilterByPending_ExcludesOtherStates()
    {
        var activeUser = BuildConfirmedUser();
        var pendingUser = BuildUser(emailConfirmedAt: null);
        SetupRepositories([activeUser, pendingUser]);

        var result = await InvokeHandler("pending");

        result.Items.Should().HaveCount(1);
        result.Items[0].Id.Should().Be(pendingUser.Id);
    }

    [Fact]
    public async Task Handle_FilterByLocked_ExcludesOtherStates()
    {
        var lockedUser = BuildLockedUser(DateTime.UtcNow.AddHours(1));
        var activeUser = BuildConfirmedUser();
        SetupRepositories([lockedUser, activeUser]);

        var result = await InvokeHandler("locked");

        result.Items.Should().HaveCount(1);
        result.Items[0].Id.Should().Be(lockedUser.Id);
    }

    [Fact]
    public async Task Handle_FilterByAll_IncludesDeletedRows_PassesIncludeDeletedTrueToRepository()
    {
        var user = BuildConfirmedUser();
        SetupRepositories([user], includeDeleted: true);

        await InvokeHandler("all");

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

        await InvokeHandler("deleted");

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

        await InvokeHandler("active");

        await _userRepository.Received(1).ListAsync(
            Arg.Any<string?>(),
            includeDeleted: false,
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NullState_TreatedAsAll_PassesIncludeDeletedTrueToRepository()
    {
        var user = BuildConfirmedUser();
        SetupRepositories([user], includeDeleted: true);

        await InvokeHandler(null);

        await _userRepository.Received(1).ListAsync(
            Arg.Any<string?>(),
            includeDeleted: true,
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Batch query (no N+1)
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_MultipleUsers_CallsBatchMethodsOnce()
    {
        var userA = BuildConfirmedUser();
        var userB = BuildConfirmedUser();
        SetupRepositories([userA, userB]);

        await InvokeHandler("all");

        await _profileRepository.Received(1)
            .GetProfilesByUserIdsAsync(Arg.Any<IReadOnlyList<Guid>>(), Arg.Any<CancellationToken>());
        await _profileRepository.Received(1)
            .GetPermissionCountsByUserIdsAsync(Arg.Any<IReadOnlyList<Guid>>(), Arg.Any<CancellationToken>());
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
        _profileRepository.GetProfilesByUserIdsAsync(
                Arg.Any<IReadOnlyList<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, IReadOnlyList<Profile>>
            {
                [user.Id] = [profileA, profileB]
            });

        var result = await InvokeHandler("all");

        result.Items[0].ProfileCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_UserWithDistinctPermissions_ReturnsResolvedPermissionCount()
    {
        var user = BuildConfirmedUser();
        SetupRepositories([user]);
        _profileRepository.GetPermissionCountsByUserIdsAsync(
                Arg.Any<IReadOnlyList<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, int> { [user.Id] = 3 });

        var result = await InvokeHandler("all");

        result.Items[0].ResolvedPermissionCount.Should().Be(3);
    }

    [Fact]
    public async Task Handle_UserWithNoProfiles_ReturnsZeroProfileAndPermissionCounts()
    {
        var user = BuildConfirmedUser();
        SetupRepositories([user]);

        var result = await InvokeHandler("all");

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

        var result = await InvokeHandler("all");

        result.Items.Should().BeEmpty();
        result.Total.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ForwardsSearchAndPagingToRepository()
    {
        SetupRepositories([]);

        var query = new ListUsersQueryHandler.Query(
            Search: "alice",
            State: "active",
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

        _profileRepository.GetProfilesByUserIdsAsync(
                Arg.Any<IReadOnlyList<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, IReadOnlyList<Profile>>());
        _profileRepository.GetPermissionCountsByUserIdsAsync(
                Arg.Any<IReadOnlyList<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, int>());

        var query = new ListUsersQueryHandler.Query(
            Search: null,
            State: null,
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

        var result = await InvokeHandler("all");

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
        string? state,
        string? search = null,
        int page = 1,
        int pageSize = 20)
    {
        var query = new ListUsersQueryHandler.Query(search, state, page, pageSize);
        return await CreateHandler().Handle(query, CancellationToken.None);
    }

    private void SetupRepositories(IReadOnlyList<User> users, bool includeDeleted = false)
    {
        _userRepository.ListAsync(
                Arg.Any<string?>(),
                Arg.Any<bool>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns((users, users.Count));

        _profileRepository.GetProfilesByUserIdsAsync(
                Arg.Any<IReadOnlyList<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, IReadOnlyList<Profile>>());

        _profileRepository.GetPermissionCountsByUserIdsAsync(
                Arg.Any<IReadOnlyList<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, int>());
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
