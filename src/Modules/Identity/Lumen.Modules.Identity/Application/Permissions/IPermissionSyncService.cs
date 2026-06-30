namespace Lumen.Modules.Identity.Application.Permissions;

public interface IPermissionSyncService
{
    Task SyncDiscoveredAsync(IReadOnlyList<DiscoveredPermissionEntry> discovered, CancellationToken ct = default);

    Task ReconcileAdministratorAsync(CancellationToken ct = default);
}

public sealed record DiscoveredPermissionEntry(
    string Controller,
    string Action,
    string DisplayName,
    string Code,
    string GroupName);
