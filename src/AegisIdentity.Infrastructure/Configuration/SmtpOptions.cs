using System.ComponentModel.DataAnnotations;

namespace AegisIdentity.Infrastructure.Configuration;

/// <summary>
/// Strongly-typed options for SMTP email delivery, bound to the "Smtp" configuration section.
/// Validated at startup via ValidateDataAnnotations + ValidateOnStart.
/// </summary>
public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    [Required(AllowEmptyStrings = false, ErrorMessage = "Smtp:Host is required.")]
    public string Host { get; init; } = string.Empty;

    [Range(1, 65535, ErrorMessage = "Smtp:Port must be between 1 and 65535.")]
    public int Port { get; init; } = 587;

    // User and Pass are intentionally not [Required] — some SMTP relays allow
    // anonymous delivery in development (e.g., Mailpit on localhost).
    public string User { get; init; } = string.Empty;

    public string Pass { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false, ErrorMessage = "Smtp:From is required.")]
    [EmailAddress(ErrorMessage = "Smtp:From must be a valid e-mail address.")]
    public string From { get; init; } = string.Empty;

    public bool UseStartTls { get; init; } = false;
}
