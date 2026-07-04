using Lumen.Authorization.Application.Queries;

namespace Lumen.Authorization.Backoffice.ViewModels;

public sealed record UserProfileAssignmentViewModel(
    Guid UserId,
    string Username,
    string Email,
    string State,
    IReadOnlyList<ListUserProfilesResult> AssignedProfiles,
    IReadOnlyList<ListProfilesResult> AvailableProfiles,
    string? ErrorMessage);
