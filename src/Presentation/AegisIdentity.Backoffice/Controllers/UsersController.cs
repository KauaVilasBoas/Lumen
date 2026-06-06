using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AegisIdentity.Backoffice.Controllers;

/// <summary>
/// Users directory with profile memberships and account lifecycle.
///
/// NOTE: the public API does not yet expose a "list users" endpoint, so this
/// controller ships with a representative demo dataset that mirrors the seeded
/// admin + sample identities. When you add <c>GET /api/users</c> (and
/// <c>GET /api/users/{id}/profiles</c> already exists), replace
/// <see cref="DemoUsers"/> with calls through <c>AdminApiClient</c>.
/// </summary>
[Authorize]
public sealed class UsersController : Controller
{
    [HttpGet]
    public IActionResult Index(Guid? id)
    {
        var users = DemoUsers();
        var selected = id.HasValue
            ? users.FirstOrDefault(u => u.Id == id.Value) ?? users[0]
            : users[0];

        var listItems = users
            .Select(u => new UserListItem(u.Id, u.Username, u.Email, u.State, u.Bootstrap, u.Color))
            .ToList();

        return View(new UsersPageModel(listItems, selected));
    }

    // ---- view models ----
    public sealed record UsersPageModel(IReadOnlyList<UserListItem> Users, UserVm Selected);

    public sealed record UserListItem(Guid Id, string Username, string Email, string State, bool Bootstrap, string Color);

    public sealed record ProfileVm(string Name, bool IsSystem, string Color, int PermCount);

    public sealed record UserVm(
        Guid Id, string Username, string Email, string State, bool Bootstrap, string Color,
        IReadOnlyList<ProfileVm> Profiles,
        int ResolvedPermissions,
        IReadOnlyList<(string t, string d, bool on, string c, string note)> Lifecycle);

    // ---- demo dataset (replace with API calls when the endpoint exists) ----
    private static List<UserVm> DemoUsers()
    {
        ProfileVm Admin() => new("Administrator", true, "#8b6dff", 14);
        ProfileVm User() => new("User", true, "#5b6478", 0);
        ProfileVm Auditor() => new("Security Auditor", false, "#4c8dff", 6);
        ProfileVm Mgr() => new("Profile Manager", false, "#2bd4a0", 6);
        ProfileVm Support() => new("Support Agent", false, "#f5a623", 6);

        (string, string, bool, string, string) Reg(string d) => ("Registered", d, true, "var(--pres)", "isActive = false until confirmed");
        (string, string, bool, string, string) Conf(string d, bool on) => ("Email confirmed", on ? d : "pending", on, "var(--app)", "");
        (string, string, bool, string, string) Last(string d, bool on) => ("Last login", on ? d : "—", on, "var(--dom)", "");
        (string, string, bool, string, string) Active() => ("Active session", "JWT valid", true, "var(--ok)", "");
        (string, string, bool, string, string) Locked(string d) => ("Locked out", d, true, "var(--danger)", "423 until lockout expires");
        (string, string, bool, string, string) Deleted(string d) => ("Soft-deleted", d, true, "var(--text-faint)", "row retained · email re-registerable");

        return new List<UserVm>
        {
            new(Guid.Parse("10000000-0000-0000-0000-000000000001"), "admin", "admin@aegisidentity.local", "active", true,
                "linear-gradient(135deg,#9a7dff,#6b49f0)", new[] { Admin() }, 14,
                new[] { Reg("180d ago"), Conf("180d ago", true), Last("4m ago", true), Active() }),

            new(Guid.Parse("a1b2c3d4-1111-4a2b-9c3d-100000000002"), "lfischer", "lena.fischer@northwind.io", "active", false,
                "linear-gradient(135deg,#4c8dff,#2a5fd6)", new[] { Auditor(), User() }, 6,
                new[] { Reg("96d ago"), Conf("96d ago", true), Last("38m ago", true), Active() }),

            new(Guid.Parse("a1b2c3d4-2222-4a2b-9c3d-100000000003"), "malvarez", "marco.alvarez@northwind.io", "active", false,
                "linear-gradient(135deg,#2bd4a0,#159e78)", new[] { Mgr(), User() }, 6,
                new[] { Reg("74d ago"), Conf("74d ago", true), Last("3h ago", true), Active() }),

            new(Guid.Parse("a1b2c3d4-3333-4a2b-9c3d-100000000004"), "pnair", "priya.nair@northwind.io", "active", false,
                "linear-gradient(135deg,#f5a623,#d4830a)", new[] { Support(), User() }, 6,
                new[] { Reg("53d ago"), Conf("53d ago", true), Last("1d ago", true), Active() }),

            new(Guid.Parse("a1b2c3d4-4444-4a2b-9c3d-100000000005"), "tbecker", "tom.becker@northwind.io", "locked", false,
                "linear-gradient(135deg,#ff5d73,#c83a52)", new[] { User() }, 0,
                new[] { Reg("40d ago"), Conf("40d ago", true), Last("2d ago", true), Locked("5 failed · 14m") }),

            new(Guid.Parse("a1b2c3d4-5555-4a2b-9c3d-100000000006"), "skhan", "sara.khan@northwind.io", "pending", false,
                "linear-gradient(135deg,#7b86a0,#525c74)", Array.Empty<ProfileVm>(), 0,
                new[] { Reg("75m ago"), Conf("", false), Last("", false), ("Awaiting confirmation", "blocked", false, "var(--warn)", "403 until email confirmed") }),

            new(Guid.Parse("a1b2c3d4-6666-4a2b-9c3d-100000000007"), "jdoe", "oldhire@northwind.io", "deleted", false,
                "linear-gradient(135deg,#4b5468,#333a4c)", new[] { User() }, 0,
                new[] { Reg("150d ago"), Conf("150d ago", true), Last("45d ago", true), Deleted("12d ago") }),
        };
    }
}
