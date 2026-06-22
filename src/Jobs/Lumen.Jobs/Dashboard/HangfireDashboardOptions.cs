namespace Lumen.Jobs.Dashboard;

/// <summary>
/// Strongly-typed options for the Hangfire dashboard, bound from the
/// <c>Hangfire:Dashboard</c> configuration section.
///
/// Populate real credentials via <c>dotnet user-secrets</c> in development or
/// environment variables in production — never commit real values.
///
/// Environment variable format (double-underscore as section separator):
/// <code>
///   Hangfire__Dashboard__Username=&lt;value&gt;
///   Hangfire__Dashboard__Password=&lt;value&gt;
///   Hangfire__Dashboard__Path=&lt;value&gt;
/// </code>
/// </summary>
public sealed class HangfireDashboardOptions
{
    public const string SectionName = "Hangfire:Dashboard";

    /// <summary>
    /// Username required to access the dashboard.
    /// Defaults to an empty string — the auth filter denies access when empty (fail-closed).
    /// </summary>
    public string Username { get; init; } = string.Empty;

    /// <summary>
    /// Password required to access the dashboard.
    /// Defaults to an empty string — the auth filter denies access when empty (fail-closed).
    /// </summary>
    public string Password { get; init; } = string.Empty;

    /// <summary>
    /// URL path at which the dashboard is mounted.
    /// Defaults to a non-obvious path to reduce exposure.
    /// Override via configuration to further obfuscate.
    /// </summary>
    public string Path { get; init; } = "/internal/jobs-admin";
}
