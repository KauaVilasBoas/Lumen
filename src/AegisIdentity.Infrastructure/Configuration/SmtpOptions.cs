using System.ComponentModel.DataAnnotations;

namespace AegisIdentity.Infrastructure.Configuration;

public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    [Required(AllowEmptyStrings = false, ErrorMessage = "Smtp:Host is required.")]
    public string Host { get; init; } = string.Empty;

    [Range(1, 65535, ErrorMessage = "Smtp:Port must be between 1 and 65535.")]
    public int Port { get; init; } = 587;

    // Not [Required]: anonymous SMTP relays are valid in dev (e.g. Mailpit on localhost).
    public string User { get; init; } = string.Empty;

    public string Pass { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false, ErrorMessage = "Smtp:From is required.")]
    [EmailAddress(ErrorMessage = "Smtp:From must be a valid e-mail address.")]
    public string From { get; init; } = string.Empty;

    public bool UseStartTls { get; init; } = false;
}
