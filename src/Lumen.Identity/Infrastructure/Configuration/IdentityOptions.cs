using System.ComponentModel.DataAnnotations;

namespace Lumen.Identity.Infrastructure.Configuration;

public sealed class IdentityJwtOptions
{
    public const string SectionName = "Jwt";

    [Required(AllowEmptyStrings = false)]
    public string Issuer { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string Audience { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    [MinLength(32)]
    public string Secret { get; init; } = string.Empty;

    [Range(1, 1440)]
    public int ExpirationMinutes { get; init; } = 15;

    [Range(1, 365)]
    public int RefreshExpirationDays { get; init; } = 7;
}

internal sealed class IdentityAppOptions
{
    public const string SectionName = "App";

    [Required(AllowEmptyStrings = false)]
    public string BaseUrl { get; init; } = string.Empty;

    [Range(1, 100)]
    public int LockoutThreshold { get; init; } = 5;

    [Range(1, 10080)]
    public int LockoutDurationMinutes { get; init; } = 15;

    [Range(1, 365)]
    public int RefreshTokenExpirationDays { get; init; } = 7;
}

/// <summary>SMTP configuration consumed by MailKit transport.</summary>
public sealed class IdentitySmtpOptions
{
    public const string SectionName = "Smtp";

    [Required(AllowEmptyStrings = false)]
    public string Host { get; init; } = string.Empty;

    [Range(1, 65535)]
    public int Port { get; init; } = 587;

    public string User { get; init; } = string.Empty;

    public string Pass { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    [EmailAddress]
    public string From { get; init; } = string.Empty;

    public bool UseStartTls { get; init; } = true;
}

internal sealed class IdentityHibpOptions
{
    public const string SectionName = "Hibp";

    [Required(AllowEmptyStrings = false)]
    public string UserAgent { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string ApiBaseUrl { get; init; } = string.Empty;
}
