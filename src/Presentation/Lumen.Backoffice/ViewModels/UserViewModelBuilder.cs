using Lumen.Backoffice.Services;
using Lumen.SharedKernel.Constants;

namespace Lumen.Backoffice.ViewModels;

public static class UserViewModelBuilder
{
    public static string AvatarColor(Guid id)
        => BackofficeCssTokens.AvatarGradients[Math.Abs(id.GetHashCode()) % BackofficeCssTokens.AvatarGradients.Length];

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
            return profile.ProfileId == SystemProfiles.AdministratorId
                ? BackofficeCssTokens.ProfileAccentAdministrator
                : BackofficeCssTokens.ProfileAccentSystemDefault;

        return BackofficeCssTokens.ProfileAccentColors[
            Math.Abs(profile.Name.GetHashCode()) % BackofficeCssTokens.ProfileAccentColors.Length];
    }

    private static IReadOnlyList<LifecycleStepViewModel> BuildLifecycle(AdminApiClient.UserDetail u)
    {
        var steps = new List<LifecycleStepViewModel>
        {
            new(BackofficeDisplayLabels.Registered,
                FormatDate(u.CreatedAt),
                true,
                BackofficeCssTokens.LifecycleColorPresentation,
                BackofficeDisplayLabels.RegisteredNote),
        };

        var emailConfirmed = u.EmailConfirmedAt.HasValue;
        steps.Add(new LifecycleStepViewModel(
            BackofficeDisplayLabels.EmailConfirmed,
            emailConfirmed ? FormatDate(u.EmailConfirmedAt!.Value) : BackofficeDisplayLabels.EmailPending,
            emailConfirmed,
            BackofficeCssTokens.LifecycleColorApplication,
            string.Empty));

        var hasLogin = u.LastLoginAt.HasValue;
        steps.Add(new LifecycleStepViewModel(
            BackofficeDisplayLabels.LastLogin,
            hasLogin ? FormatDate(u.LastLoginAt!.Value) : BackofficeDisplayLabels.NoLoginDate,
            hasLogin,
            BackofficeCssTokens.LifecycleColorDomain,
            string.Empty));

        steps.Add(u.State switch
        {
            UserStates.Locked => new LifecycleStepViewModel(
                BackofficeDisplayLabels.LockedOut,
                u.LockoutEndAt.HasValue ? FormatDate(u.LockoutEndAt!.Value) : BackofficeDisplayLabels.LockoutIndefinite,
                true,
                BackofficeCssTokens.LifecycleColorDanger,
                BackofficeDisplayLabels.LockoutNote),

            UserStates.Deleted => new LifecycleStepViewModel(
                BackofficeDisplayLabels.SoftDeleted,
                BackofficeDisplayLabels.SoftDeletedDate,
                true,
                BackofficeCssTokens.LifecycleColorFaint,
                BackofficeDisplayLabels.SoftDeletedNote),

            UserStates.Pending => new LifecycleStepViewModel(
                BackofficeDisplayLabels.AwaitingConfirmation,
                BackofficeDisplayLabels.AwaitingBlocked,
                false,
                BackofficeCssTokens.LifecycleColorWarning,
                BackofficeDisplayLabels.AwaitingNote),

            _ => new LifecycleStepViewModel(
                BackofficeDisplayLabels.ActiveSession,
                BackofficeDisplayLabels.ActiveSessionDate,
                true,
                BackofficeCssTokens.LifecycleColorOk,
                string.Empty),
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
