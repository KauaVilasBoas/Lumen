namespace Lumen.Modules.Identity.Domain.Security;

internal interface IPasswordValidator
{
    Task<PasswordValidationResult> ValidatePasswordAsync(
        PasswordValidationContext context,
        CancellationToken ct = default);
}
