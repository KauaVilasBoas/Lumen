using Hangfire.Dashboard;

namespace AegisIdentity.Jobs.Dashboard;

/// <summary>
/// Hangfire dashboard authorization filter that grants access only to
/// authenticated users.
///
/// The Backoffice uses cookie authentication; any user who holds a valid
/// Backoffice session cookie is considered authenticated.
///
/// TODO(AUTH-ROLES): once a role/permission system is in place, add a
/// claim check here so only users with the "admin" role can access the
/// dashboard (e.g. context.GetHttpContext().User.IsInRole("admin")).
/// </summary>
public sealed class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        return httpContext.User.Identity?.IsAuthenticated == true;
    }
}
