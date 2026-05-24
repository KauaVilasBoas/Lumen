namespace AegisIdentity.Domain.Security;

/// <summary>
/// Input context for password policy validation.
/// Carries the candidate password together with the user's identity attributes
/// so that validators can reject passwords that equal the user's email or username.
/// </summary>
public sealed record PasswordValidationContext(string Password, string Email, string Username);
