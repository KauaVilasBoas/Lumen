using System.ComponentModel.DataAnnotations;

namespace Lumen.Backoffice.Configuration;

/// <summary>
/// Typed options for the upstream Lumen API that the Backoffice calls.
/// Bound from the "Api" section in appsettings.json.
/// </summary>
public sealed class BackofficeApiOptions
{
    public const string SectionName = "Api";

    // Base URL of the Lumen API. No trailing slash.
    // Example: "https://api.lumen.io"
    // Override via dotnet user-secrets (dev) or env var Api__BaseUrl (prod).
    [Required(AllowEmptyStrings = false, ErrorMessage = "Api:BaseUrl is required.")]
    [Url(ErrorMessage = "Api:BaseUrl must be a valid absolute URL.")]
    public string BaseUrl { get; init; } = string.Empty;
}