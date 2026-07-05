using Lumen.Authorization.Application.Queries;

namespace Lumen.Authorization.Backoffice.ViewModels;

public sealed record ProfileDetailViewModel(
    GetProfileResult Profile,
    IReadOnlyList<ListPermissionsGroupResult> AllPermissions,
    string? ErrorMessage);
