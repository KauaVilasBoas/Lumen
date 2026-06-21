namespace AegisIdentity.SharedKernel.Constants;

/// <summary>
/// Canonical names of the transactional email templates embedded in the Infrastructure assembly.
/// These strings are passed to <c>IEmailTemplateRenderer.Render</c> and must exactly match the
/// embedded resource file names (without extension) under <c>Templates/Email/</c>.
/// </summary>
public static class EmailTemplateNames
{
    /// <summary>Sent to new users to verify their email address after registration.</summary>
    public const string EmailConfirmation = "EmailConfirmation";

    /// <summary>Sent when a user requests a password-reset link.</summary>
    public const string PasswordReset = "PasswordReset";

    /// <summary>Sent after a user successfully changes their password.</summary>
    public const string PasswordChanged = "PasswordChanged";
}
