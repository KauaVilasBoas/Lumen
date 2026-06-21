using AegisIdentity.Backoffice.Services;

namespace AegisIdentity.Backoffice.ViewModels;

public sealed record ProfileDetailViewModel(
    AdminApiClient.ProfileDetail Profile,
    IReadOnlyList<AdminApiClient.PermissionGroup> AllPermissions);
