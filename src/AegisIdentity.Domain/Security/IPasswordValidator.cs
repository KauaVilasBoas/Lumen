namespace AegisIdentity.Domain.Security;

/// <summary>
/// Port for password policy validation (complexity, HIBP check, etc.).
/// Defined in Domain so that command handlers declare only the abstraction;
/// the FluentValidation-based implementation lives in Application.
/// </summary>
public interface IPasswordValidator
{
    Task<PasswordValidationResult> ValidatePasswordAsync(
        PasswordValidationContext context,
        CancellationToken ct = default);
}
