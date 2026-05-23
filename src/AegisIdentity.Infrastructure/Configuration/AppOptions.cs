using System.ComponentModel.DataAnnotations;

namespace AegisIdentity.Infrastructure.Configuration;

public sealed class AppOptions
{
    public const string SectionName = "App";

    // Base URL of this API — used to build absolute links in outbound emails.
    // Example: "https://api.aegisidentity.io" (no trailing slash).
    [Required(AllowEmptyStrings = false, ErrorMessage = "App:BaseUrl is required.")]
    [Url(ErrorMessage = "App:BaseUrl must be a valid absolute URL.")]
    public string BaseUrl { get; init; } = string.Empty;
}
