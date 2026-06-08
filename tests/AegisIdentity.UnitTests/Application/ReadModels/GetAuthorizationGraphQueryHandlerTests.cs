using AegisIdentity.Domain.Authorization;
using AegisIdentity.Domain.Users;
using AegisIdentity.ReadModels.Queries;
using FluentAssertions;
using NSubstitute;

namespace AegisIdentity.UnitTests.Application.ReadModels;

public sealed class GetAuthorizationGraphQueryHandlerTests
{
    private readonly IUserRepository            _userRepository            = Substitute.For<IUserRepository>();
    private readonly IUserProfileRepository     _userProfileRepository     = Substitute.For<IUserProfileRepository>();
    private readonly IProfileRepository         _profileRepository         = Substitute.For<IProfileRepository>();
    private readonly IPermissionRepository      _permissionRepository      = Substitute.For<IPermissionRepository>();
    private readonly IGroupPermissionRepository _groupPermissionRepository = Substitute.For<IGroupPermissionRepository>();

    // ──────────────────────────────────────────────────────────────────────────
    // Empty graph
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_NoData_ReturnsEmptySnapshot()
    {
        SetupEmptyRepositories();

        var result = await InvokeHandler();

        result.Users.Should().BeEmpty();
        result.Profiles.Should().BeEmpty();
        result.Permissions.Should().BeEmpty();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // User nodes
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ActiveUser_ReturnsUserNodeWithStateActive()
    {
        var user = BuildConfirmedUser();
        SetupRepositoriesWithUsers([user]);

        var result = await InvokeHandler();

        result.Users.Should().HaveCount(1);
        result.Users[0].Id.Should().Be(user.Id);
        result.Users[0].Username.Should().Be(user.Username);
        result.Users[0].Email.Should().Be(user.Email);
        result.Users[0].State.Should().Be("active");
    }

    [Fact]
    public async Task Handle_PendingUser_ReturnsStateAsPending()
    {
        var user = BuildUser(emailConfirmedAt: null);
        SetupRepositoriesWithUsers([user]);

        var result = await InvokeHandler();

        result.Users[0].State.Should().Be("pending");
    }

    [Fact]
    public async Task Handle_LockedUser_ReturnsStateAsLocked()
    {
        var user = BuildLockedUser(DateTime.UtcNow.AddHours(1));
        SetupRepositoriesWithUsers([user]);

        var result = await InvokeHandler();

        result.Users[0].State.Should().Be("locked");
    }

    [Fact]
    public async Task Handle_UserWithProfile_IncludesProfileIdInUserNode()
    {
        var user    = BuildConfirmedUser();
        var profile = Profile.Create("Admin", "Full access");

        SetupRepositoriesWithUsers([user]);
        _profileRepository.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns([profile]);
        _userProfileRepository.ListByUserIdAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns([UserProfile.Create(user.Id, profile.Id)]);
        _profileRepository.GetActivePermissionProfilesByProfileIdAsync(profile.Id, Arg.Any<CancellationToken>())
            .Returns([]);

        var result = await InvokeHandler();

        result.Users[0].Profiles.Should().ContainSingle(id => id == profile.Id.ToString());
    }

    [Fact]
    public async Task Handle_UserWithNoProfiles_ReturnsEmptyProfilesList()
    {
        var user = BuildConfirmedUser();
        SetupRepositoriesWithUsers([user]);
        _userProfileRepository.ListByUserIdAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns([]);

        var result = await InvokeHandler();

        result.Users[0].Profiles.Should().BeEmpty();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Profile nodes
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ProfileWithPermission_BuildsProfileNodeWithPermissionId()
    {
        var profile    = Profile.Create("Viewer", "Read only");
        var permission = Permission.Create("Users", "List", "Users.List");

        SetupEmptyRepositories();
        _profileRepository.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns([profile]);
        _permissionRepository.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns([permission]);
        _profileRepository.GetActivePermissionProfilesByProfileIdAsync(profile.Id, Arg.Any<CancellationToken>())
            .Returns([PermissionProfile.Create(permission.Id, profile.Id)]);

        var result = await InvokeHandler();

        result.Profiles.Should().ContainKey(profile.Id.ToString());
        var profileNode = result.Profiles[profile.Id.ToString()];
        profileNode.Name.Should().Be("Viewer");
        profileNode.IsSystem.Should().BeFalse();
        profileNode.Permissions.Should().ContainSingle(id => id == permission.Id.ToString());
    }

    [Fact]
    public async Task Handle_SystemProfile_SetsIsSystemTrue()
    {
        var profile = Profile.Create("Bootstrap", "System", isSystem: true);

        SetupEmptyRepositories();
        _profileRepository.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns([profile]);
        _profileRepository.GetActivePermissionProfilesByProfileIdAsync(profile.Id, Arg.Any<CancellationToken>())
            .Returns([]);

        var result = await InvokeHandler();

        result.Profiles[profile.Id.ToString()].IsSystem.Should().BeTrue();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Permission nodes
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_PermissionWithGroup_ReturnsGroupNameInNode()
    {
        var group      = GroupPermission.Create("Authorization", "Authorization group");
        var permission = Permission.Create("Authorization", "Graph.View", "Authorization.Graph.View", group.Id);

        SetupEmptyRepositories();
        _permissionRepository.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns([permission]);
        _groupPermissionRepository.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns([group]);

        var result = await InvokeHandler();

        var permNode = result.Permissions[permission.Id.ToString()];
        permNode.Code.Should().Be("Authorization.Graph.View");
        permNode.Group.Should().Be("Authorization");
        permNode.Orphan.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_OrphanPermission_SetsOrphanTrue()
    {
        var permission = Permission.Create("Legacy", "Action", "Legacy.Action");
        permission.MarkAsOrphan();

        SetupEmptyRepositories();
        _permissionRepository.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns([permission]);

        var result = await InvokeHandler();

        result.Permissions[permission.Id.ToString()].Orphan.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_PermissionWithoutGroup_ReturnsEmptyGroupString()
    {
        var permission = Permission.Create("Users", "List", "Users.List");

        SetupEmptyRepositories();
        _permissionRepository.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns([permission]);

        var result = await InvokeHandler();

        result.Permissions[permission.Id.ToString()].Group.Should().BeEmpty();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Shape contract — no color, no method fields in result records
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void UserNode_DoesNotExposeColorOrMethod()
    {
        var userNodeType = typeof(GetAuthorizationGraphQueryHandler.UserNode);

        userNodeType.GetProperty("Color").Should().BeNull("color is a presentation concern");
        userNodeType.GetProperty("Method").Should().BeNull("method is derivable at the front end");
    }

    [Fact]
    public void PermissionNode_DoesNotExposeColorOrMethod()
    {
        var permNodeType = typeof(GetAuthorizationGraphQueryHandler.PermissionNode);

        permNodeType.GetProperty("Color").Should().BeNull("color is a presentation concern");
        permNodeType.GetProperty("Method").Should().BeNull("method is derivable at the front end");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────────

    private Task<GetAuthorizationGraphQueryHandler.GraphSnapshot> InvokeHandler()
    {
        var handler = new GetAuthorizationGraphQueryHandler(
            _userRepository,
            _userProfileRepository,
            _profileRepository,
            _permissionRepository,
            _groupPermissionRepository);

        return handler.Handle(new GetAuthorizationGraphQueryHandler.Query(), CancellationToken.None);
    }

    private void SetupEmptyRepositories()
    {
        _userRepository.ListAsync(
                Arg.Any<string?>(),
                Arg.Any<bool>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<User>)[], 0));

        _profileRepository.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns([]);

        _permissionRepository.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns([]);

        _groupPermissionRepository.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns([]);
    }

    private void SetupRepositoriesWithUsers(IReadOnlyList<User> users)
    {
        _userRepository.ListAsync(
                Arg.Any<string?>(),
                Arg.Any<bool>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<User>)users, users.Count));

        _profileRepository.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns([]);

        _permissionRepository.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns([]);

        _groupPermissionRepository.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns([]);

        foreach (var user in users)
        {
            _userProfileRepository.ListByUserIdAsync(user.Id, Arg.Any<CancellationToken>())
                .Returns([]);
        }
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
}
