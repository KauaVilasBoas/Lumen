using System.ComponentModel.DataAnnotations;

namespace AegisIdentity.Backoffice.Configuration;

/// <summary>
/// Typed options for the upstream AegisIdentity API that the Backoffice calls.
/// Bound from the "Api" section in appsettings.json.
/// </summary>
public sealed class BackofficeApiOptions
{
    public const string SectionName = "Api";

    // Base URL of the AegisIdentity API. No trailing slash.
    // Example: "https://api.aegisidentity.io"
    // Override via dotnet user-secrets (dev) or env var Api__BaseUrl (prod).
    [Required(AllowEmptyStrings = false, ErrorMessage = "Api:BaseUrl is required.")]
    [Url(ErrorMessage = "Api:BaseUrl must be a valid absolute URL.")]
    public string BaseUrl { get; init; } = string.Empty;
}