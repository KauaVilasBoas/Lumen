using AegisIdentity.Backoffice.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AegisIdentity.Backoffice.Controllers;

[Authorize]
public sealed class UsersController : BackofficeBaseController
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

    private readonly AdminApiClient _adminApiClient;

    public UsersController(AdminApiClient adminApiClient)
    {
        _adminApiClient = adminApiClient;
    }

    [HttpGet]
    public async Task<IActionResult> Index(Guid? id, CancellationToken ct)
    {
        var page = await _adminApiClient.ListUsersAsync(
            search: null, state: null, page: 1, pageSize: 100, ct);

        var users = page?.Items ?? [];

        if (users.Count == 0)
            return View(new UsersPageModel([], null));

        var firstId = id.HasValue && users.Any(u => u.Id == id.Value)
            ? id.Value
            : users[0].Id;

        var selected = await _adminApiClient.GetUserAsync(firstId, ct);

        if (selected is null)
            return View(new UsersPageModel([], null));

        var listItems = users
            .Select(u => new UserListItem(u.Id, u.Username, u.Email, u.State, u.IsBootstrap, AvatarColor(u.Id)))
            .ToList();

        return View(new UsersPageModel(listItems, MapDetail(selected)));
    }

    // ---- view models ----

    public sealed record UsersPageModel(IReadOnlyList<UserListItem> Users, UserVm? Selected);

    public sealed record UserListItem(Guid Id, string Username, string Email, string State, bool Bootstrap, string Color);

    public sealed record ProfileVm(string Name, bool IsSystem, string Color, int PermCount);

    public sealed record UserVm(
        Guid Id, string Username, string Email, string State, bool Bootstrap, string Color,
        IReadOnlyList<ProfileVm> Profiles,
        int ResolvedPermissions,
        IReadOnlyList<(string t, string d, bool on, string c, string note)> Lifecycle);

    // ---- private helpers ----

    private static string AvatarColor(Guid id)
        => AvatarPalette[Math.Abs(id.GetHashCode()) % AvatarPalette.Length];

    private static string ProfileAccentColor(AdminApiClient.ProfileMembership profile)
    {
        if (profile.IsSystem)
            return profile.Name == "Administrator" ? "#8b6dff" : "#5b6478";

        var palette = new[] { "#4c8dff", "#2bd4a0", "#f5a623", "#f25fa6", "#a78bfa" };
        return palette[Math.Abs(profile.Name.GetHashCode()) % palette.Length];
    }

    private static UserVm MapDetail(AdminApiClient.UserDetail detail)
    {
        var profiles = detail.Profiles
            .Select(p => new ProfileVm(p.Name, p.IsSystem, ProfileAccentColor(p), p.PermissionCount))
            .ToList();

        return new UserVm(
            detail.Id,
            detail.Username,
            detail.Email,
            detail.State,
            detail.IsBootstrap,
            AvatarColor(detail.Id),
            profiles,
            detail.ResolvedPermissionCount,
            BuildLifecycle(detail));
    }

    private static IReadOnlyList<(string t, string d, bool on, string c, string note)> BuildLifecycle(
        AdminApiClient.UserDetail u)
    {
        var steps = new List<(string, string, bool, string, string)>
        {
            ("Registered", FormatDate(u.CreatedAt), true, "var(--pres)", "isActive = false until confirmed"),
        };

        var emailConfirmed = u.EmailConfirmedAt.HasValue;
        steps.Add(("Email confirmed",
            emailConfirmed ? FormatDate(u.EmailConfirmedAt!.Value) : "pending",
            emailConfirmed,
            "var(--app)",
            ""));

        var hasLogin = u.LastLoginAt.HasValue;
        steps.Add(("Last login",
            hasLogin ? FormatDate(u.LastLoginAt!.Value) : "—",
            hasLogin,
            "var(--dom)",
            ""));

        steps.Add(u.State switch
        {
            "locked" => ("Locked out",
                u.LockoutEndAt.HasValue ? FormatDate(u.LockoutEndAt!.Value) : "indefinite",
                true,
                "var(--danger)",
                "423 until lockout expires"),

            "deleted" => ("Soft-deleted",
                "account removed",
                true,
                "var(--text-faint)",
                "row retained · email re-registerable"),

            "pending" => ("Awaiting confirmation",
                "blocked",
                false,
                "var(--warn)",
                "403 until email confirmed"),

            _ => ("Active session", "JWT valid", true, "var(--ok)", ""),
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
