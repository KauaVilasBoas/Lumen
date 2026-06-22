using Lumen.Backoffice.Services;

namespace Lumen.Backoffice.ViewModels;

public sealed record ProfileDetailViewModel(
    AdminApiClient.ProfileDetail Profile,
    IReadOnlyList<AdminApiClient.PermissionGroup> AllPermissions);
