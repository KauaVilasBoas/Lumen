namespace AegisIdentity.SharedKernel.Constants;

/// <summary>
/// Placeholder keys used inside email template bodies.
/// The renderer replaces <c>{{Key}}</c> tokens; these constants must match the
/// double-brace tokens written in the embedded <c>.html</c> and <c>.txt</c> files.
/// </summary>
public static class EmailPlaceholderKeys
{
    /// <summary>The recipient's display username, e.g. <c>jdoe</c>.</summary>
    public const string UserName = "UserName";

    /// <summary>The full URL the user must visit to confirm their email address.</summary>
    public const string ConfirmationUrl = "ConfirmationUrl";

    /// <summary>The full URL the user must visit to reset their password.</summary>
    public const string ResetUrl = "ResetUrl";
}
