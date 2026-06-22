namespace Lumen.Domain.Configuration;

/// <summary>
/// Exposes application-level settings to command handlers without coupling them
/// to a specific configuration provider or IOptions&lt;T&gt;.
/// </summary>
public interface IAppSettings
{
    /// <summary>
    /// Public base URL of this API (e.g. "https://api.aegisidentity.io").
    /// Used to build absolute links included in outbound emails.
    /// </summary>
    string BaseUrl { get; }

    /// <summary>
    /// Number of consecutive failed login attempts before the account is locked.
    /// </summary>
    int LockoutThreshold { get; }

    /// <summary>
    /// How long an account remains locked after reaching the lockout threshold.
    /// </summary>
    TimeSpan LockoutDuration { get; }

    /// <summary>
    /// Lifetime of the refresh token in days.
    /// </summary>
    int RefreshTokenExpirationDays { get; }
}
