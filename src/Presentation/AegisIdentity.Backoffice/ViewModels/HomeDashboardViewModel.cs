using AegisIdentity.Backoffice.Services;

namespace AegisIdentity.Backoffice.ViewModels;

public sealed record HomeDashboardViewModel(
    int? UserCount,
    int? ProfileCount,
    int? PermissionCount,
    int? OrphanCount,
    double? CacheHitRate,
    IReadOnlyList<AdminApiClient.AuditEntry>? Activity,
    AdminApiClient.JobStats? JobStats);
