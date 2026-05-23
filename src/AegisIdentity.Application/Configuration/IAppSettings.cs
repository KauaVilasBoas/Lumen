namespace AegisIdentity.Application.Configuration;

/// <summary>
/// Exposes application-level settings to use cases without coupling them
/// to a specific configuration provider or IOptions&lt;T&gt;.
/// </summary>
public interface IAppSettings
{
    /// <summary>
    /// Public base URL of this API (e.g. "https://api.aegisidentity.io").
    /// Used to build absolute links included in outbound emails.
    /// </summary>
    string BaseUrl { get; }
}
