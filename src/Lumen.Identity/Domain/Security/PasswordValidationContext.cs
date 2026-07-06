namespace Lumen.Identity.Domain.Security;

public sealed record PasswordValidationContext(string Password, string Email, string Username);
