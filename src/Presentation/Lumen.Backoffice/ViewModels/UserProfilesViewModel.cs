using Lumen.Backoffice.Services;

namespace Lumen.Backoffice.ViewModels;

public sealed record UserProfilesViewModel(
    Guid UserId,
    IReadOnlyList<AdminApiClient.UserProfileItem> AssignedProfiles,
    IReadOnlyList<AdminApiClient.ProfileItem> AvailableProfiles);
