using Lumen.Modules.Identity.Domain.Authorization;
using Lumen.Modules.Identity.Domain.Users;
using MediatR;

namespace Lumen.Modules.Identity.Application.Queries;

internal sealed class GetAuthorizationGraphQueryHandler
    : IRequestHandler<GetAuthorizationGraphQueryHandler.Query, GetAuthorizationGraphQueryHandler.GraphSnapshot>
{
    public sealed record Query : IRequest<GraphSnapshot>;

    public sealed record UserNode(
        Guid Id,
        string Username,
        string Email,
        string State,
        IReadOnlyList<string> Profiles);

    public sealed record ProfileNode(
        string Name,
        bool IsSystem,
        IReadOnlyList<string> Permissions);

    public sealed record PermissionNode(
        string Code,
        string Name,
        string Group,
        bool Orphan);

    public sealed record GraphSnapshot(
        IReadOnlyList<UserNode> Users,
        IReadOnlyDictionary<string, ProfileNode> Profiles,
        IReadOnlyDictionary<string, PermissionNode> Permissions);

    private readonly IUserRepository _userRepository;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IProfileRepository _profileRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IGroupPermissionRepository _groupPermissionRepository;

    public GetAuthorizationGraphQueryHandler(
        IUserRepository userRepository,
        IUserProfileRepository userProfileRepository,
        IProfileRepository profileRepository,
        IPermissionRepository permissionRepository,
        IGroupPermissionRepository groupPermissionRepository)
    {
        _userRepository = userRepository;
        _userProfileRepository = userProfileRepository;
        _profileRepository = profileRepository;
        _permissionRepository = permissionRepository;
        _groupPermissionRepository = groupPermissionRepository;
    }

    public async Task<GraphSnapshot> Handle(Query query, CancellationToken ct)
    {
        var (rawUsers, _) = await _userRepository.ListAsync(
            search: null,
            includeDeleted: false,
            page: 1,
            pageSize: int.MaxValue,
            ct);

        var profiles    = await _profileRepository.ListAllAsync(ct);
        var permissions = await _permissionRepository.ListAllAsync(ct);
        var groups      = await _groupPermissionRepository.ListAllAsync(ct);

        var groupNameById = groups.ToDictionary(g => g.Id, g => g.Name);
        var profileById   = profiles.ToDictionary(p => p.Id);

        var permissionNodes  = BuildPermissionNodes(permissions, groupNameById);
        var profileNodes     = await BuildProfileNodesAsync(profiles, permissionNodes, ct);
        var userNodes        = await BuildUserNodesAsync(rawUsers, profileById, ct);

        return new GraphSnapshot(userNodes, profileNodes, permissionNodes);
    }

    private static Dictionary<string, PermissionNode> BuildPermissionNodes(
        IReadOnlyList<Permission> permissions,
        Dictionary<Guid, string> groupNameById)
    {
        var result = new Dictionary<string, PermissionNode>(permissions.Count);

        foreach (var p in permissions)
        {
            var groupName = p.GroupPermissionId.HasValue && groupNameById.TryGetValue(p.GroupPermissionId.Value, out var name)
                ? name
                : string.Empty;

            result[p.Id.ToString()] = new PermissionNode(
                Code:   p.Code,
                Name:   p.DisplayName,
                Group:  groupName,
                Orphan: p.IsOrphan);
        }

        return result;
    }

    private async Task<Dictionary<string, ProfileNode>> BuildProfileNodesAsync(
        IReadOnlyList<Profile> profiles,
        Dictionary<string, PermissionNode> permissionNodes,
        CancellationToken ct)
    {
        var result = new Dictionary<string, ProfileNode>(profiles.Count);

        if (profiles.Count == 0)
            return result;

        var profileIds = profiles.Select(p => p.Id).ToList();
        var permissionProfilesByProfile = await _profileRepository
            .GetActivePermissionProfilesByProfileIdsAsync(profileIds, ct);

        foreach (var profile in profiles)
        {
            var permissionProfiles = permissionProfilesByProfile.TryGetValue(profile.Id, out var pps)
                ? pps
                : [];

            var permissionIds = permissionProfiles
                .Select(pp => pp.PermissionId.ToString())
                .Where(permissionNodes.ContainsKey)
                .ToList();

            result[profile.Id.ToString()] = new ProfileNode(
                Name:        profile.Name,
                IsSystem:    profile.IsSystem,
                Permissions: permissionIds);
        }

        return result;
    }

    private async Task<IReadOnlyList<UserNode>> BuildUserNodesAsync(
        IReadOnlyList<User> users,
        Dictionary<Guid, Profile> profileById,
        CancellationToken ct)
    {
        if (users.Count == 0)
            return [];

        var now    = DateTime.UtcNow;
        var userIds = users.Select(u => u.Id).ToList();
        var userProfilesByUser = await _userProfileRepository.ListByUserIdsAsync(userIds, ct);

        return users
            .Select(user =>
            {
                var userProfiles = userProfilesByUser.TryGetValue(user.Id, out var ups) ? ups : [];

                var profileIds = userProfiles
                    .Select(up => up.ProfileId)
                    .Where(profileById.ContainsKey)
                    .Select(id => id.ToString())
                    .ToList();

                return new UserNode(
                    Id:       user.Id,
                    Username: user.Username,
                    Email:    user.Email,
                    State:    UserStateResolver.Resolve(user, now),
                    Profiles: profileIds);
            })
            .ToList();
    }
}
