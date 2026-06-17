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

    [Fact]
    public async Task Handle_NoData_ReturnsEmptySnapshot()
    {
        SetupEmptyRepositories();

        var result = await InvokeHandler();

        result.Users.Should().BeEmpty();
        result.Profiles.Should().BeEmpty();
        result.Permissions.Should().BeEmpty();
    }

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
        _userProfileRepository.ListByUserIdsAsync(
                Arg.Any<IReadOnlyList<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, IReadOnlyList<UserProfile>>
            {
                [user.Id] = [UserProfile.Create(user.Id, profile.Id)]
            });
        _profileRepository.GetActivePermissionProfilesByProfileIdsAsync(
                Arg.Any<IReadOnlyList<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, IReadOnlyList<PermissionProfile>>());

        var result = await InvokeHandler();

        result.Users[0].Profiles.Should().ContainSingle(id => id == profile.Id.ToString());
    }

    [Fact]
    public async Task Handle_UserWithNoProfiles_ReturnsEmptyProfilesList()
    {
        var user = BuildConfirmedUser();
        SetupRepositoriesWithUsers([user]);
        _userProfileRepository.ListByUserIdsAsync(
                Arg.Any<IReadOnlyList<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, IReadOnlyList<UserProfile>>());

        var result = await InvokeHandler();

        result.Users[0].Profiles.Should().BeEmpty();
    }

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
        _profileRepository.GetActivePermissionProfilesByProfileIdsAsync(
                Arg.Any<IReadOnlyList<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, IReadOnlyList<PermissionProfile>>
            {
                [profile.Id] = [PermissionProfile.Create(permission.Id, profile.Id)]
            });

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
        _profileRepository.GetActivePermissionProfilesByProfileIdsAsync(
                Arg.Any<IReadOnlyList<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, IReadOnlyList<PermissionProfile>>());

        var result = await InvokeHandler();

        result.Profiles[profile.Id.ToString()].IsSystem.Should().BeTrue();
    }

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

    [Fact]
    public async Task Handle_MultipleUsersAndProfiles_UsesBatchQueriesAndMapsCorrectly()
    {
        var user1 = BuildConfirmedUser();
        var user2 = BuildConfirmedUser();
        var profile1 = Profile.Create("Admin", "Full access");
        var profile2 = Profile.Create("Viewer", "Read only");
        var permission = Permission.Create("Users", "List", "Users.List");

        SetupEmptyRepositories();
        _userRepository.ListAsync(
                Arg.Any<string?>(),
                Arg.Any<bool>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<User>)[user1, user2], 2));

        _profileRepository.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns([profile1, profile2]);

        _permissionRepository.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns([permission]);

        _userProfileRepository.ListByUserIdsAsync(
                Arg.Any<IReadOnlyList<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, IReadOnlyList<UserProfile>>
            {
                [user1.Id] = [UserProfile.Create(user1.Id, profile1.Id)],
                [user2.Id] = [UserProfile.Create(user2.Id, profile2.Id)]
            });

        _profileRepository.GetActivePermissionProfilesByProfileIdsAsync(
                Arg.Any<IReadOnlyList<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, IReadOnlyList<PermissionProfile>>
            {
                [profile1.Id] = [PermissionProfile.Create(permission.Id, profile1.Id)],
                [profile2.Id] = []
            });

        var result = await InvokeHandler();

        result.Users.Should().HaveCount(2);
        result.Users.Should().Contain(u => u.Id == user1.Id && u.Profiles.Contains(profile1.Id.ToString()));
        result.Users.Should().Contain(u => u.Id == user2.Id && u.Profiles.Contains(profile2.Id.ToString()));
        result.Profiles[profile1.Id.ToString()].Permissions.Should().ContainSingle();
        result.Profiles[profile2.Id.ToString()].Permissions.Should().BeEmpty();
    }

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

        _profileRepository.GetActivePermissionProfilesByProfileIdsAsync(
                Arg.Any<IReadOnlyList<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, IReadOnlyList<PermissionProfile>>());

        _userProfileRepository.ListByUserIdsAsync(
                Arg.Any<IReadOnlyList<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, IReadOnlyList<UserProfile>>());
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

        _profileRepository.GetActivePermissionProfilesByProfileIdsAsync(
                Arg.Any<IReadOnlyList<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, IReadOnlyList<PermissionProfile>>());

        _userProfileRepository.ListByUserIdsAsync(
                Arg.Any<IReadOnlyList<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, IReadOnlyList<UserProfile>>());
    }

    private static User BuildUser(DateTime? emailConfirmedAt = null)
    {
        var user = User.Create(
            email: $"{Guid.NewGuid():N}@example.com",
            username: $"user-{Guid.NewGuid():N}",
            passwordHash: "hashed");

        SetUserProperty(user, nameof(User.EmailConfirmedAt), emailConfirmedAt);
        return user;
    }

    private static User BuildConfirmedUser()
        => BuildUser(emailConfirmedAt: DateTime.UtcNow.AddDays(-1));

    private static User BuildLockedUser(DateTime lockedUntil)
    {
        var user = BuildConfirmedUser();
        SetUserProperty(user, nameof(User.LockedUntil), lockedUntil);
        return user;
    }

    private static void SetUserProperty(User user, string propertyName, object? value)
        => typeof(User).GetProperty(propertyName)!.SetValue(user, value);
}
