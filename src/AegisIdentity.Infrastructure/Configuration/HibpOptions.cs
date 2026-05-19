using System.ComponentModel.DataAnnotations;

namespace AegisIdentity.Infrastructure.Configuration;

/// <summary>
/// Strongly-typed options for Have I Been Pwned (HIBP) API integration,
/// bound to the "Hibp" configuration section.
/// </summary>
public sealed class HibpOptions
{
    public const string SectionName = "Hibp";

    [Required(AllowEmptyStrings = false, ErrorMessage = "Hibp:UserAgent is required.")]
    public string UserAgent { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false, ErrorMessage = "Hibp:ApiBaseUrl is required.")]
    [Url(ErrorMessage = "Hibp:ApiBaseUrl must be a valid URL.")]
    public string ApiBaseUrl { get; init; } = string.Empty;
}
