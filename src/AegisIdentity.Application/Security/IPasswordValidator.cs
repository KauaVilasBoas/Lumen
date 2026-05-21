namespace AegisIdentity.Application.Security;

public interface IPasswordValidator
{
    Task<PasswordValidationResult> ValidatePasswordAsync(PasswordValidationContext context, CancellationToken ct = default);
}
