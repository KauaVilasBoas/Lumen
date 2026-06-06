using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AegisIdentity.Backoffice.Controllers;

/// <summary>
/// The live Authorization Graph. Serialises the authorization model
/// (users → profiles → permissions) to JSON consumed by aegis-console.js,
/// which renders the interactive node graph client-side.
///
/// NOTE: ships with a representative demo dataset (same shape as the seeded
/// data). When a "list users with profiles" endpoint exists, build
/// <see cref="BuildGraph"/> from <c>AdminApiClient</c> instead. The JSON shape is:
///   { users:[{id,username,email,color,state,profiles:[profileId]}],
///     profiles:{ id:{name,color,isSystem,permissions:[permId]} },
///     permissions:{ id:{code,name,method,group,orphan} } }
/// </summary>
[Authorize]
public sealed class AuthorizationGraphController : Controller
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    [HttpGet]
    public IActionResult Index()
    {
        ViewBag.GraphJson = JsonSerializer.Serialize(BuildGraph(), JsonOpts);
        return View();
    }

    private static object BuildGraph()
    {
        // permission catalogue (id -> definition)
        var permissions = new Dictionary<string, object>
        {
            ["p_prof_index"]  = new { code = "Profiles.Index",          name = "List profiles",            method = "GET",    group = "Profiles",     orphan = false },
            ["p_prof_get"]    = new { code = "Profiles.Get",            name = "View profile",             method = "GET",    group = "Profiles",     orphan = false },
            ["p_prof_create"] = new { code = "Profiles.Create",         name = "Create profile",           method = "POST",   group = "Profiles",     orphan = false },
            ["p_prof_update"] = new { code = "Profiles.Update",         name = "Update profile",           method = "PUT",    group = "Profiles",     orphan = false },
            ["p_prof_delete"] = new { code = "Profiles.Delete",         name = "Soft-delete profile",      method = "DELETE", group = "Profiles",     orphan = false },
            ["p_prof_setp"]   = new { code = "Profiles.SetPermissions", name = "Set profile permissions",  method = "PUT",    group = "Profiles",     orphan = false },
            ["p_perm_index"]  = new { code = "Permissions.Index",       name = "List permissions",         method = "GET",    group = "Permissions",  orphan = false },
            ["p_up_index"]    = new { code = "UserProfiles.Index",      name = "List user assignments",    method = "GET",    group = "UserProfiles", orphan = false },
            ["p_up_assign"]   = new { code = "UserProfiles.Assign",     name = "Assign profile to user",   method = "POST",   group = "UserProfiles", orphan = false },
            ["p_up_remove"]   = new { code = "UserProfiles.Remove",     name = "Remove profile from user", method = "DELETE", group = "UserProfiles", orphan = false },
            ["p_usr_index"]   = new { code = "Users.Index",             name = "List users",               method = "GET",    group = "Users",        orphan = false },
            ["p_usr_get"]     = new { code = "Users.Get",               name = "View user",                method = "GET",    group = "Users",        orphan = false },
            ["p_usr_delete"]  = new { code = "Users.Delete",            name = "Soft-delete user",         method = "DELETE", group = "Users",        orphan = false },
            ["p_adm_ping"]    = new { code = "Admin.Ping",              name = "Diagnostics ping",         method = "GET",    group = "Admin",        orphan = false },
            ["p_rep_export"]  = new { code = "Reports.Export",          name = "Export report",            method = "GET",    group = "Reports",      orphan = true  },
        };

        var allNonOrphan = permissions.Where(kv => kv.Key != "p_rep_export").Select(kv => kv.Key).ToArray();

        var profiles = new Dictionary<string, object>
        {
            ["pr_admin"]    = new { name = "Administrator",   color = "#8b6dff", isSystem = true,  permissions = allNonOrphan.Append("p_rep_export").ToArray() },
            ["pr_user"]     = new { name = "User",            color = "#5b6478", isSystem = true,  permissions = Array.Empty<string>() },
            ["pr_secaudit"] = new { name = "Security Auditor", color = "#4c8dff", isSystem = false, permissions = new[] { "p_prof_index", "p_prof_get", "p_perm_index", "p_up_index", "p_usr_index", "p_usr_get" } },
            ["pr_profmgr"]  = new { name = "Profile Manager",  color = "#2bd4a0", isSystem = false, permissions = new[] { "p_prof_index", "p_prof_get", "p_prof_create", "p_prof_update", "p_prof_setp", "p_perm_index" } },
            ["pr_support"]  = new { name = "Support Agent",    color = "#f5a623", isSystem = false, permissions = new[] { "p_usr_index", "p_usr_get", "p_up_index", "p_up_assign", "p_up_remove", "p_prof_index" } },
        };

        var users = new[]
        {
            new { id = "10000000-0000-0000-0000-000000000001", username = "admin",    email = "admin@aegisidentity.local",    color = "linear-gradient(135deg,#9a7dff,#6b49f0)", state = "active",  profiles = new[] { "pr_admin" } },
            new { id = "a1b2c3d4-1111-4a2b-9c3d-100000000002",  username = "lfischer", email = "lena.fischer@northwind.io",    color = "linear-gradient(135deg,#4c8dff,#2a5fd6)", state = "active",  profiles = new[] { "pr_secaudit", "pr_user" } },
            new { id = "a1b2c3d4-2222-4a2b-9c3d-100000000003",  username = "malvarez", email = "marco.alvarez@northwind.io",   color = "linear-gradient(135deg,#2bd4a0,#159e78)", state = "active",  profiles = new[] { "pr_profmgr", "pr_user" } },
            new { id = "a1b2c3d4-3333-4a2b-9c3d-100000000004",  username = "pnair",    email = "priya.nair@northwind.io",      color = "linear-gradient(135deg,#f5a623,#d4830a)", state = "active",  profiles = new[] { "pr_support", "pr_user" } },
            new { id = "a1b2c3d4-4444-4a2b-9c3d-100000000005",  username = "tbecker",  email = "tom.becker@northwind.io",      color = "linear-gradient(135deg,#ff5d73,#c83a52)", state = "locked",  profiles = new[] { "pr_user" } },
            new { id = "a1b2c3d4-5555-4a2b-9c3d-100000000006",  username = "skhan",    email = "sara.khan@northwind.io",       color = "linear-gradient(135deg,#7b86a0,#525c74)", state = "pending", profiles = Array.Empty<string>() },
        };

        return new { users, profiles, permissions };
    }
}
