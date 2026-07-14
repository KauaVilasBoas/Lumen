using Lumen.Backoffice.Services;

namespace Lumen.Backoffice.ViewModels;

public sealed record HomeDashboardViewModel(
    int? UserCount,
    int? ProfileCount,
    int? PermissionCount,
    double? CacheHitRate,
    IReadOnlyList<AdminApiClient.AuditEntry>? Activity,
    AdminApiClient.JobStats? JobStats);
