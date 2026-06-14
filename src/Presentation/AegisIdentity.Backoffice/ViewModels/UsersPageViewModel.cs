namespace AegisIdentity.Backoffice.ViewModels;

public sealed record UsersPageViewModel(
    IReadOnlyList<UserListItemViewModel> Users,
    UserDetailViewModel? Selected);

public sealed record UserListItemViewModel(
    Guid Id,
    string Username,
    string Email,
    string State,
    bool Bootstrap,
    string AvatarColor);

public sealed record ProfileMembershipViewModel(
    string Name,
    bool IsSystem,
    string AccentColor,
    int PermissionCount);

public sealed record LifecycleStepViewModel(
    string Title,
    string Date,
    bool IsCompleted,
    string Color,
    string Note);

public sealed record UserDetailViewModel(
    Guid Id,
    string Username,
    string Email,
    string State,
    bool Bootstrap,
    string AvatarColor,
    IReadOnlyList<ProfileMembershipViewModel> Profiles,
    int ResolvedPermissions,
    IReadOnlyList<LifecycleStepViewModel> Lifecycle);
