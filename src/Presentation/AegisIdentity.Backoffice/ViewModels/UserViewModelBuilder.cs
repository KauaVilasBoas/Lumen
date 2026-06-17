using AegisIdentity.Backoffice.Services;
using AegisIdentity.SharedKernel.Constants;

namespace AegisIdentity.Backoffice.ViewModels;

public static class UserViewModelBuilder
{
    private static readonly string[] AvatarPalette =
    [
        "linear-gradient(135deg,#9a7dff,#6b49f0)",
        "linear-gradient(135deg,#4c8dff,#2a5fd6)",
        "linear-gradient(135deg,#2bd4a0,#159e78)",
        "linear-gradient(135deg,#f5a623,#d4830a)",
        "linear-gradient(135deg,#f25fa6,#c43e87)",
        "linear-gradient(135deg,#a78bfa,#7c59e0)",
        "linear-gradient(135deg,#34d399,#059669)",
    ];

    private static readonly string[] ProfileColorPalette =
        ["#4c8dff", "#2bd4a0", "#f5a623", "#f25fa6", "#a78bfa"];

    public static string AvatarColor(Guid id)
        => AvatarPalette[Math.Abs(id.GetHashCode()) % AvatarPalette.Length];

    public static UserListItemViewModel ToListItem(AdminApiClient.UserListItem u)
        => new(u.Id, u.Username, u.Email, u.State, u.IsBootstrap, AvatarColor(u.Id));

    public static UserDetailViewModel ToDetail(AdminApiClient.UserDetail detail)
        => new(
            Id:                  detail.Id,
            Username:            detail.Username,
            Email:               detail.Email,
            State:               detail.State,
            Bootstrap:           detail.IsBootstrap,
            AvatarColor:         AvatarColor(detail.Id),
            Profiles:            detail.Profiles.Select(ToProfileMembership).ToList(),
            ResolvedPermissions: detail.ResolvedPermissionCount,
            Lifecycle:           BuildLifecycle(detail));

    private static ProfileMembershipViewModel ToProfileMembership(AdminApiClient.ProfileMembership p)
        => new(p.Name, p.IsSystem, ProfileAccentColor(p), p.PermissionCount);

    private static string ProfileAccentColor(AdminApiClient.ProfileMembership profile)
    {
        if (profile.IsSystem)
            return profile.ProfileId == SystemProfiles.AdministratorId ? "#8b6dff" : "#5b6478";

        return ProfileColorPalette[Math.Abs(profile.Name.GetHashCode()) % ProfileColorPalette.Length];
    }

    private static IReadOnlyList<LifecycleStepViewModel> BuildLifecycle(AdminApiClient.UserDetail u)
    {
        var steps = new List<LifecycleStepViewModel>
        {
            new("Registered", FormatDate(u.CreatedAt), true, "var(--pres)", "isActive = false until confirmed"),
        };

        var emailConfirmed = u.EmailConfirmedAt.HasValue;
        steps.Add(new LifecycleStepViewModel(
            "Email confirmed",
            emailConfirmed ? FormatDate(u.EmailConfirmedAt!.Value) : "pending",
            emailConfirmed,
            "var(--app)",
            ""));

        var hasLogin = u.LastLoginAt.HasValue;
        steps.Add(new LifecycleStepViewModel(
            "Last login",
            hasLogin ? FormatDate(u.LastLoginAt!.Value) : "—",
            hasLogin,
            "var(--dom)",
            ""));

        steps.Add(u.State switch
        {
            UserStates.Locked => new LifecycleStepViewModel(
                "Locked out",
                u.LockoutEndAt.HasValue ? FormatDate(u.LockoutEndAt!.Value) : "indefinite",
                true,
                "var(--danger)",
                "423 until lockout expires"),

            UserStates.Deleted => new LifecycleStepViewModel(
                "Soft-deleted",
                "account removed",
                true,
                "var(--text-faint)",
                "row retained · email re-registerable"),

            UserStates.Pending => new LifecycleStepViewModel(
                "Awaiting confirmation",
                "blocked",
                false,
                "var(--warn)",
                "403 until email confirmed"),

            _ => new LifecycleStepViewModel("Active session", "JWT valid", true, "var(--ok)", ""),
        });

        return steps;
    }

    private static string FormatDate(DateTime utc)
    {
        var diff = DateTime.UtcNow - utc;

        if (diff.TotalMinutes < 60)
            return $"{(int)diff.TotalMinutes}m ago";

        if (diff.TotalHours < 24)
            return $"{(int)diff.TotalHours}h ago";

        if (diff.TotalDays < 365)
            return $"{(int)diff.TotalDays}d ago";

        return utc.ToString("yyyy-MM-dd");
    }
}
