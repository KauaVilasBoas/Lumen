namespace AegisIdentity.SharedKernel.Constants;

/// <summary>
/// Numeric bounds used by validators across the application.
/// Centralised here so that the presentation layer (validators) and the
/// command handlers share the same values without duplication.
/// </summary>
public static class ValidationLimits
{
    // ── Username ──────────────────────────────────────────────────────────────

    public const int UsernameMinLength = 3;

    public const int UsernameMaxLength = 32;

    public const int EmailMaxLength = 256;

    // ── Password ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Minimum number of characters required by the password policy.
    /// Also used by <c>PasswordValidator</c> at runtime.
    /// </summary>
    public const int PasswordMinLength = 12;

    /// <summary>
    /// Accepted special characters for the password complexity rule.
    /// </summary>
    public const string PasswordSpecialCharacters = "!@#$%^&*()-_=+[]{};:'\",.<>/?\\|`~";
}
