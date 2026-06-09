using AegisIdentity.Domain.Authorization;
using AegisIdentity.Domain.Users;
using AegisIdentity.ReadModels.Queries;
using AegisIdentity.ReadModels.Users;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace AegisIdentity.Api.Hubs;

public sealed class GraphLivePushHandler : INotificationHandler<UserPermissionsChanged>
{
    private readonly IHubContext<AuthorizationGraphHub, IAuthorizationGraphHubClient> _hub;
    private readonly IUserRepository _userRepository;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IProfileRepository _profileRepository;
    private readonly ILogger<GraphLivePushHandler> _logger;

    public GraphLivePushHandler(
        IHubContext<AuthorizationGraphHub, IAuthorizationGraphHubClient> hub,
        IUserRepository userRepository,
        IUserProfileRepository userProfileRepository,
        IProfileRepository profileRepository,
        ILogger<GraphLivePushHandler> logger)
    {
        _hub = hub;
        _userRepository = userRepository;
        _userProfileRepository = userProfileRepository;
        _profileRepository = profileRepository;
        _logger = logger;
    }

    public async Task Handle(UserPermissionsChanged notification, CancellationToken cancellationToken)
    {
        var user = await _userRepository.FindByIdAsync(notification.UserId, cancellationToken);

        if (user is null)
        {
            _logger.LogWarning(
                "GraphLivePushHandler: user {UserId} not found — skipping hub push.",
                notification.UserId);
            return;
        }

        var delta = await BuildUserDeltaAsync(user, cancellationToken);

        await _hub.Clients
            .User(notification.UserId.ToString())
            .GraphUpdated(delta);

        _logger.LogInformation(
            "GraphUpdated pushed to hub for user {UserId} with {ProfileCount} profile(s).",
            notification.UserId,
            delta.Users.Count > 0 ? delta.Users[0].Profiles.Count : 0);
    }

    private async Task<GetAuthorizationGraphQueryHandler.GraphSnapshot> BuildUserDeltaAsync(
        User user,
        CancellationToken cancellationToken)
    {
        var userProfiles = await _userProfileRepository.ListByUserIdAsync(user.Id, cancellationToken);
        var profileIds = userProfiles.Select(up => up.ProfileId).ToList();
        var profiles = await _profileRepository.GetByIdsAsync(profileIds, cancellationToken);

        var profileNodes = new Dictionary<string, GetAuthorizationGraphQueryHandler.ProfileNode>();

        foreach (var profile in profiles)
        {
            var permissionProfiles = await _profileRepository
                .GetActivePermissionProfilesByProfileIdAsync(profile.Id, cancellationToken);

            var permissionIdStrings = permissionProfiles
                .Select(pp => pp.PermissionId.ToString())
                .ToList();

            profileNodes[profile.Id.ToString()] = new GetAuthorizationGraphQueryHandler.ProfileNode(
                Name: profile.Name,
                IsSystem: profile.IsSystem,
                Permissions: permissionIdStrings);
        }

        var resolvedPermissionIds = profileNodes.Values
            .SelectMany(p => p.Permissions)
            .Distinct()
            .ToDictionary(
                id => id,
                _ => new GetAuthorizationGraphQueryHandler.PermissionNode(
                    Code: string.Empty,
                    Name: string.Empty,
                    Group: string.Empty,
                    Orphan: false));

        var userNode = new GetAuthorizationGraphQueryHandler.UserNode(
            Id: user.Id,
            Username: user.Username,
            Email: user.Email,
            State: UserStateResolver.Resolve(user, DateTime.UtcNow),
            Profiles: profileIds.Select(id => id.ToString()).ToList());

        return new GetAuthorizationGraphQueryHandler.GraphSnapshot(
            Users: [userNode],
            Profiles: profileNodes,
            Permissions: resolvedPermissionIds);
    }
}
