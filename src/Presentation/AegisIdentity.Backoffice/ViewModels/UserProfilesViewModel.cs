using AegisIdentity.Backoffice.Services;

namespace AegisIdentity.Backoffice.ViewModels;

public sealed record UserProfilesViewModel(
    Guid UserId,
    IReadOnlyList<AdminApiClient.UserProfileItem> AssignedProfiles,
    IReadOnlyList<AdminApiClient.ProfileItem> AvailableProfiles);
