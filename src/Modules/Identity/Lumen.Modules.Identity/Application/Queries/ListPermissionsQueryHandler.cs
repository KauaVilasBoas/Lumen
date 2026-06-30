using Lumen.Modules.Identity.Domain.Authorization;
using MediatR;

namespace Lumen.Modules.Identity.Application.Queries;

public sealed record ListPermissionsQuery : IRequest<IReadOnlyList<ListPermissionsGroupResult>>;

public sealed record ListPermissionsPermissionResult(
    Guid Id,
    string Code,
    string DisplayName,
    bool IsOrphan);

public sealed record ListPermissionsGroupResult(
    Guid? GroupId,
    string GroupName,
    IReadOnlyList<ListPermissionsPermissionResult> Permissions);

internal sealed class ListPermissionsQueryHandler
    : IRequestHandler<ListPermissionsQuery, IReadOnlyList<ListPermissionsGroupResult>>
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IGroupPermissionRepository _groupPermissionRepository;

    public ListPermissionsQueryHandler(
        IPermissionRepository permissionRepository,
        IGroupPermissionRepository groupPermissionRepository)
    {
        _permissionRepository = permissionRepository;
        _groupPermissionRepository = groupPermissionRepository;
    }

    public async Task<IReadOnlyList<ListPermissionsGroupResult>> Handle(ListPermissionsQuery query, CancellationToken ct)
    {
        var permissions = await _permissionRepository.ListAllAsync(ct);
        var groups = await _groupPermissionRepository.ListAllAsync(ct);

        var groupById = groups.ToDictionary(g => g.Id);

        var grouped = permissions
            .GroupBy(p => p.GroupPermissionId)
            .Select(g =>
            {
                var groupName = g.Key.HasValue && groupById.TryGetValue(g.Key.Value, out var grp)
                    ? grp.Name
                    : "Ungrouped";

                var items = g
                    .Select(p => new ListPermissionsPermissionResult(p.Id, p.Code, p.DisplayName, p.IsOrphan))
                    .OrderBy(p => p.Code)
                    .ToList();

                return new ListPermissionsGroupResult(g.Key, groupName, items);
            })
            .OrderBy(g => g.GroupName)
            .ToList();

        return grouped;
    }
}
