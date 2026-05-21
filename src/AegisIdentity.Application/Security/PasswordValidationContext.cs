namespace AegisIdentity.Application.Security;

public sealed record PasswordValidationContext(string Password, string Email, string Username);
