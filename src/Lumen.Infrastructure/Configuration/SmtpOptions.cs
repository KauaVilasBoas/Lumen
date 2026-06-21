using System.ComponentModel.DataAnnotations;

namespace AegisIdentity.Infrastructure.Configuration;

public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    [Required(AllowEmptyStrings = false, ErrorMessage = "Smtp:Host is required.")]
    public string Host { get; init; } = string.Empty;

    [Range(1, 65535, ErrorMessage = "Smtp:Port must be between 1 and 65535.")]
    public int Port { get; init; } = 587;

    /// <summary>
    /// Not <c>[Required]</c>: anonymous SMTP relays are valid in dev (e.g. Mailpit on localhost).
    /// Production requires it via <see cref="SmtpProductionOptionsValidator"/>.
    /// </summary>
    public string User { get; init; } = string.Empty;

    public string Pass { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false, ErrorMessage = "Smtp:From is required.")]
    [EmailAddress(ErrorMessage = "Smtp:From must be a valid e-mail address.")]
    public string From { get; init; } = string.Empty;

    /// <summary>
    /// Secure by default: dev relays without STARTTLS (e.g. Mailpit) opt out explicitly.
    /// </summary>
    public bool UseStartTls { get; init; } = true;
}
