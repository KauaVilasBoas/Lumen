using System.ComponentModel.DataAnnotations;

namespace Lumen.Infrastructure.Configuration;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required(AllowEmptyStrings = false, ErrorMessage = "Jwt:Issuer is required.")]
    public string Issuer { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false, ErrorMessage = "Jwt:Audience is required.")]
    public string Audience { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false, ErrorMessage = "Jwt:Secret is required.")]
    [MinLength(32, ErrorMessage = "Jwt:Secret must be at least 32 characters for HS256.")]
    public string Secret { get; init; } = string.Empty;

    [Range(1, 1440, ErrorMessage = "Jwt:ExpirationMinutes must be between 1 and 1440.")]
    public int ExpirationMinutes { get; init; } = 15;

    [Range(1, 365, ErrorMessage = "Jwt:RefreshExpirationDays must be between 1 and 365.")]
    public int RefreshExpirationDays { get; init; } = 7;
}
