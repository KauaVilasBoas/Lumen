using AegisIdentity.Domain.Authorization;
using MediatR;

namespace AegisIdentity.ReadModels.Queries;

public sealed class ListPermissionsQueryHandler
    : IRequestHandler<ListPermissionsQueryHandler.Query, IReadOnlyList<ListPermissionsQueryHandler.GroupResult>>
{
    public sealed record Query : IRequest<IReadOnlyList<GroupResult>>;

    public sealed record PermissionResult(
        Guid Id,
        string Code,
        string DisplayName,
        bool IsOrphan);

    public sealed record GroupResult(
        Guid? GroupId,
        string GroupName,
        IReadOnlyList<PermissionResult> Permissions);

    private readonly IPermissionRepository _permissionRepository;
    private readonly IGroupPermissionRepository _groupPermissionRepository;

    public ListPermissionsQueryHandler(
        IPermissionRepository permissionRepository,
        IGroupPermissionRepository groupPermissionRepository)
    {
        _permissionRepository = permissionRepository;
        _groupPermissionRepository = groupPermissionRepository;
    }

    public async Task<IReadOnlyList<GroupResult>> Handle(Query query, CancellationToken ct)
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
                    .Select(p => new PermissionResult(p.Id, p.Code, p.DisplayName, p.IsOrphan))
                    .OrderBy(p => p.Code)
                    .ToList();

                return new GroupResult(g.Key, groupName, items);
            })
            .OrderBy(g => g.GroupName)
            .ToList();

        return grouped;
    }
}
