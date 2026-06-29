namespace Lumen.Modules.Identity.Domain.Security;

internal sealed record PasswordValidationContext(string Password, string Email, string Username);
