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

    // Number of consecutive failed logins before the account is temporarily locked.
    [Range(1, 100, ErrorMessage = "App:LockoutThreshold must be between 1 and 100.")]
    public int LockoutThreshold { get; init; } = 5;

    // Duration (in minutes) for which an account remains locked after reaching the threshold.
    [Range(1, 10080, ErrorMessage = "App:LockoutDurationMinutes must be between 1 and 10080.")]
    public int LockoutDurationMinutes { get; init; } = 15;

    // How many days a refresh token is valid before the user must log in again.
    [Range(1, 365, ErrorMessage = "App:RefreshTokenExpirationDays must be between 1 and 365.")]
    public int RefreshTokenExpirationDays { get; init; } = 7;
}
