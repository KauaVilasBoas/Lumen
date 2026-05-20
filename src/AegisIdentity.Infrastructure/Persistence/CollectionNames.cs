namespace AegisIdentity.Infrastructure.Persistence;

/// <summary>
/// Central registry of MongoDB collection names.
/// Using constants avoids magic strings scattered across repositories and index initializers.
/// </summary>
public static class CollectionNames
{
    public const string Users = "users";
    public const string RefreshTokens = "refresh_tokens";
    public const string PasswordResetTokens = "password_reset_tokens";
    public const string EmailConfirmationTokens = "email_confirmation_tokens";
}
