namespace Lumen.SharedKernel.Constants;

/// <summary>
/// Sentinel values used in committed configuration files. Startup validators reject
/// these in Production so a copy-pasted template can never reach a live environment.
/// </summary>
public static class ConfigurationPlaceholders
{
    /// <summary>
    /// Placeholder committed in <c>appsettings.json</c> for values the operator must
    /// override via environment variables or secrets.
    /// </summary>
    public const string ReplaceMe = "REPLACE_ME";

    /// <summary>
    /// Host names that resolve to the local machine — valid for dev relays such as
    /// Mailpit, never for a Production SMTP server.
    /// </summary>
    public static readonly IReadOnlyList<string> LoopbackHostAliases = ["localhost", "127.0.0.1", "::1"];
}
