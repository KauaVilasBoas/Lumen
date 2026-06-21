using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AegisIdentity.Jobs.Dashboard;

/// <summary>
/// Extension method that mounts the Hangfire dashboard with Basic
/// Authentication and a configurable, non-obvious URL path.
///
/// Usage in Program.cs (after <c>builder.Build()</c>):
/// <code>
/// app.UseAegisDashboard();
/// </code>
///
/// The mount path defaults to <c>/internal/jobs-admin</c> and can be
/// overridden via <c>Hangfire:Dashboard:Path</c> in configuration.
/// </summary>
public static class HangfireDashboardApplicationBuilderExtensions
{
    /// <summary>
    /// Mounts the Hangfire dashboard at the path defined in
    /// <see cref="HangfireDashboardOptions.Path"/>, protected by
    /// <see cref="HangfireDashboardAuthorizationFilter"/>.
    /// </summary>
    public static WebApplication UseAegisDashboard(this WebApplication app)
    {
        var options      = app.Services.GetRequiredService<IOptions<HangfireDashboardOptions>>().Value;
        var authFilter   = app.Services.GetRequiredService<HangfireDashboardAuthorizationFilter>();

        app.UseHangfireDashboard(options.Path, new DashboardOptions
        {
            Authorization  = [authFilter],
            // Dashboard is read-only from the Backoffice perspective — job
            // execution is handled exclusively by the Api's Hangfire server.
            IsReadOnlyFunc = _ => false,
        });

        return app;
    }
}
