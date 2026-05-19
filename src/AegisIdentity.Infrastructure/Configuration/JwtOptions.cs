using System.ComponentModel.DataAnnotations;

namespace AegisIdentity.Infrastructure.Configuration;

/// <summary>
/// Strongly-typed options for JWT token generation, bound to the "Jwt" configuration section.
/// Placed in Infrastructure because the concrete token-generation adapter lives here.
/// Validated at startup via ValidateDataAnnotations + ValidateOnStart.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required(AllowEmptyStrings = false, ErrorMessage = "Jwt:Issuer is required.")]
    public string Issuer { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false, ErrorMessage = "Jwt:Audience is required.")]
    public string Audience { get; init; } = string.Empty;

    /// <summary>
    /// HMAC-SHA256 signing key. Must be at least 32 characters (256 bits) for HS256.
    /// Set via User Secrets in development; via env var Jwt__Secret in production.
    /// </summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "Jwt:Secret is required.")]
    [MinLength(32, ErrorMessage = "Jwt:Secret must be at least 32 characters for HS256.")]
    public string Secret { get; init; } = string.Empty;

    [Range(1, 1440, ErrorMessage = "Jwt:ExpirationMinutes must be between 1 and 1440.")]
    public int ExpirationMinutes { get; init; } = 15;

    [Range(1, 365, ErrorMessage = "Jwt:RefreshExpirationDays must be between 1 and 365.")]
    public int RefreshExpirationDays { get; init; } = 7;
}
