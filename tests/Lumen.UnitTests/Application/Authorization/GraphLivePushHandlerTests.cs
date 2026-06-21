using Lumen.Api.Hubs;
using Lumen.Domain.Authorization;
using Lumen.Domain.Users;
using Lumen.ReadModels.Queries;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Lumen.UnitTests.Application.Authorization;

public sealed class GraphLivePushHandlerTests
{
    private readonly IHubContext<AuthorizationGraphHub, IAuthorizationGraphHubClient> _hubContext;
    private readonly IAuthorizationGraphHubClient _hubClients;
    private readonly IUserRepository _userRepository;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IProfileRepository _profileRepository;
    private readonly GraphLivePushHandler _sut;

    public GraphLivePushHandlerTests()
    {
        _hubContext = Substitute.For<IHubContext<AuthorizationGraphHub, IAuthorizationGraphHubClient>>();
        _hubClients = Substitute.For<IAuthorizationGraphHubClient>();
        _userRepository = Substitute.For<IUserRepository>();
        _userProfileRepository = Substitute.For<IUserProfileRepository>();
        _profileRepository = Substitute.For<IProfileRepository>();

        _hubContext.Clients.User(Arg.Any<string>()).Returns(_hubClients);

        _sut = new GraphLivePushHandler(
            _hubContext,
            _userRepository,
            _userProfileRepository,
            _profileRepository,
            NullLogger<GraphLivePushHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WhenUserExists_PushesGraphUpdatedToUserGroup()
    {
        var userId = Guid.NewGuid();
        var permissionId = Guid.NewGuid();

        var user = User.Create("alice@example.com", "alice", "hash");
        var profile = Profile.Create("Admins", "Admin profile");
        var userProfile = UserProfile.Create(user.Id, profile.Id);
        var permissionProfile = PermissionProfile.Create(permissionId, profile.Id);

        _userRepository.FindByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(user);

        _userProfileRepository.ListByUserIdAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns([userProfile]);

        _profileRepository.GetByIdsAsync(Arg.Any<IReadOnlyList<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([profile]);

        _profileRepository
            .GetActivePermissionProfilesByProfileIdAsync(profile.Id, Arg.Any<CancellationToken>())
            .Returns([permissionProfile]);

        GetAuthorizationGraphQueryHandler.GraphSnapshot? captured = null;
        await _hubClients.GraphUpdated(Arg.Do<GetAuthorizationGraphQueryHandler.GraphSnapshot>(s => captured = s));

        await _sut.Handle(new UserPermissionsChanged(userId), CancellationToken.None);

        _hubContext.Clients.Received(1).User(userId.ToString());
        captured.Should().NotBeNull();
        captured!.Users.Should().HaveCount(1);
        captured.Users[0].Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_DoesNotPushToHub()
    {
        var userId = Guid.NewGuid();

        _userRepository.FindByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns((User?)null);

        await _sut.Handle(new UserPermissionsChanged(userId), CancellationToken.None);

        _hubContext.Clients.DidNotReceive().User(Arg.Any<string>());
        await _hubClients.DidNotReceive().GraphUpdated(Arg.Any<GetAuthorizationGraphQueryHandler.GraphSnapshot>());
    }

    [Fact]
    public async Task Handle_WhenUserHasNoProfiles_PushesEmptyProfilesSnapshot()
    {
        var userId = Guid.NewGuid();
        var user = User.Create("bob@example.com", "bob", "hash");

        _userRepository.FindByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(user);

        _userProfileRepository.ListByUserIdAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<UserProfile>());

        _profileRepository.GetByIdsAsync(Arg.Any<IReadOnlyList<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Profile>());

        GetAuthorizationGraphQueryHandler.GraphSnapshot? captured = null;
        await _hubClients.GraphUpdated(Arg.Do<GetAuthorizationGraphQueryHandler.GraphSnapshot>(s => captured = s));

        await _sut.Handle(new UserPermissionsChanged(userId), CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Users[0].Profiles.Should().BeEmpty();
        captured.Profiles.Should().BeEmpty();
        captured.Permissions.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenUserHasMultipleProfiles_IncludesAllProfilesInSnapshot()
    {
        var userId = Guid.NewGuid();
        var permissionId = Guid.NewGuid();

        var user = User.Create("carol@example.com", "carol", "hash");
        var profile1 = Profile.Create("Admins", "Admin profile");
        var profile2 = Profile.Create("Viewers", "Viewer profile");
        var userProfile1 = UserProfile.Create(user.Id, profile1.Id);
        var userProfile2 = UserProfile.Create(user.Id, profile2.Id);
        var permissionProfile = PermissionProfile.Create(permissionId, profile1.Id);

        _userRepository.FindByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(user);

        _userProfileRepository.ListByUserIdAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns([userProfile1, userProfile2]);

        _profileRepository.GetByIdsAsync(Arg.Any<IReadOnlyList<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([profile1, profile2]);

        _profileRepository
            .GetActivePermissionProfilesByProfileIdAsync(profile1.Id, Arg.Any<CancellationToken>())
            .Returns([permissionProfile]);

        _profileRepository
            .GetActivePermissionProfilesByProfileIdAsync(profile2.Id, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<PermissionProfile>());

        GetAuthorizationGraphQueryHandler.GraphSnapshot? captured = null;
        await _hubClients.GraphUpdated(Arg.Do<GetAuthorizationGraphQueryHandler.GraphSnapshot>(s => captured = s));

        await _sut.Handle(new UserPermissionsChanged(userId), CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Users.Should().HaveCount(1);
        captured.Users[0].Profiles.Should().HaveCount(2);
        captured.Profiles.Should().HaveCount(2);
        captured.Permissions.Should().HaveCount(1);
    }
}
