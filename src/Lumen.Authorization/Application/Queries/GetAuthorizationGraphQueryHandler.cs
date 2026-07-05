using Lumen.Authorization.Contracts;
using Lumen.Authorization.Domain;
using MediatR;

namespace Lumen.Authorization.Application.Queries;

public sealed record GetAuthorizationGraphQuery : IRequest<AuthorizationGraphSnapshot>;

public sealed record AuthorizationGraphUserNode(
    Guid Id,
    string Username,
    string Email,
    string State,
    IReadOnlyList<string> Profiles);

public sealed record AuthorizationGraphProfileNode(
    string Name,
    bool IsSystem,
    IReadOnlyList<string> Permissions);

public sealed record AuthorizationGraphPermissionNode(
    string Code,
    string Name,
    string Group,
    bool Orphan);

public sealed record AuthorizationGraphSnapshot(
    IReadOnlyList<AuthorizationGraphUserNode> Users,
    IReadOnlyDictionary<string, AuthorizationGraphProfileNode> Profiles,
    IReadOnlyDictionary<string, AuthorizationGraphPermissionNode> Permissions);

internal sealed class GetAuthorizationGraphQueryHandler
    : IRequestHandler<GetAuthorizationGraphQuery, AuthorizationGraphSnapshot>
{
    private readonly IAuthorizationUserSource _userSource;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IProfileRepository _profileRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IGroupPermissionRepository _groupPermissionRepository;

    public GetAuthorizationGraphQueryHandler(
        IAuthorizationUserSource userSource,
        IUserProfileRepository userProfileRepository,
        IProfileRepository profileRepository,
        IPermissionRepository permissionRepository,
        IGroupPermissionRepository groupPermissionRepository)
    {
        _userSource = userSource;
        _userProfileRepository = userProfileRepository;
        _profileRepository = profileRepository;
        _permissionRepository = permissionRepository;
        _groupPermissionRepository = groupPermissionRepository;
    }

    public async Task<AuthorizationGraphSnapshot> Handle(GetAuthorizationGraphQuery query, CancellationToken ct)
    {
        var rawUsers = await _userSource.ListActiveUsersAsync(ct);

        var profiles    = await _profileRepository.ListAllAsync(ct);
        var permissions = await _permissionRepository.ListAllAsync(ct);
        var groups      = await _groupPermissionRepository.ListAllAsync(ct);

        var groupNameById = groups.ToDictionary(g => g.Id, g => g.Name);
        var profileById   = profiles.ToDictionary(p => p.Id);

        var permissionNodes  = BuildPermissionNodes(permissions, groupNameById);
        var profileNodes     = await BuildProfileNodesAsync(profiles, permissionNodes, ct);
        var userNodes        = await BuildUserNodesAsync(rawUsers, profileById, ct);

        return new AuthorizationGraphSnapshot(userNodes, profileNodes, permissionNodes);
    }

    private static Dictionary<string, AuthorizationGraphPermissionNode> BuildPermissionNodes(
        IReadOnlyList<Permission> permissions,
        Dictionary<Guid, string> groupNameById)
    {
        var result = new Dictionary<string, AuthorizationGraphPermissionNode>(permissions.Count);

        foreach (var p in permissions)
        {
            var groupName = p.GroupPermissionId.HasValue && groupNameById.TryGetValue(p.GroupPermissionId.Value, out var name)
                ? name
                : string.Empty;

            result[p.Id.ToString()] = new AuthorizationGraphPermissionNode(
                Code:   p.Code,
                Name:   p.DisplayName,
                Group:  groupName,
                Orphan: p.IsOrphan);
        }

        return result;
    }

    private async Task<Dictionary<string, AuthorizationGraphProfileNode>> BuildProfileNodesAsync(
        IReadOnlyList<Profile> profiles,
        Dictionary<string, AuthorizationGraphPermissionNode> permissionNodes,
        CancellationToken ct)
    {
        var result = new Dictionary<string, AuthorizationGraphProfileNode>(profiles.Count);

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

            result[profile.Id.ToString()] = new AuthorizationGraphProfileNode(
                Name:        profile.Name,
                IsSystem:    profile.IsSystem,
                Permissions: permissionIds);
        }

        return result;
    }

    private async Task<IReadOnlyList<AuthorizationGraphUserNode>> BuildUserNodesAsync(
        IReadOnlyList<AuthorizationUserDto> users,
        Dictionary<Guid, Profile> profileById,
        CancellationToken ct)
    {
        if (users.Count == 0)
            return [];

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

                return new AuthorizationGraphUserNode(
                    Id:       user.Id,
                    Username: user.Username,
                    Email:    user.Email,
                    State:    user.State,
                    Profiles: profileIds);
            })
            .ToList();
    }
}
