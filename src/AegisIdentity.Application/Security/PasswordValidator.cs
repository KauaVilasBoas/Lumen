using AegisIdentity.Domain.Security;
using AegisIdentity.SharedKernel.Constants;
using FluentValidation;

namespace AegisIdentity.Application.Security;

public sealed class PasswordValidator : AbstractValidator<PasswordValidationContext>, IPasswordValidator
{
    private readonly IPwnedPasswordsClient _pwnedPasswordsClient;

    public PasswordValidator(IPwnedPasswordsClient pwnedPasswordsClient)
    {
        _pwnedPasswordsClient = pwnedPasswordsClient;

        RuleFor(x => x.Password)
            .Must(p => !string.IsNullOrEmpty(p) && p.Length >= ValidationLimits.PasswordMinLength)
            .WithMessage(AuthErrorMessages.PasswordTooShort);

        RuleFor(x => x.Password)
            .Must(p => !string.IsNullOrEmpty(p) && p.Any(char.IsUpper))
            .WithMessage(AuthErrorMessages.PasswordMissingUppercase);

        RuleFor(x => x.Password)
            .Must(p => !string.IsNullOrEmpty(p) && p.Any(char.IsLower))
            .WithMessage(AuthErrorMessages.PasswordMissingLowercase);

        RuleFor(x => x.Password)
            .Must(p => !string.IsNullOrEmpty(p) && p.Any(char.IsDigit))
            .WithMessage(AuthErrorMessages.PasswordMissingDigit);

        RuleFor(x => x.Password)
            .Must(p => !string.IsNullOrEmpty(p) && p.Any(IsSpecialCharacter))
            .WithMessage(AuthErrorMessages.PasswordMissingSpecial);

        RuleFor(x => x)
            .Must(NotMatchIdentity)
            .WithMessage(AuthErrorMessages.PasswordEqualsIdentity);

        // HIBP check is the most expensive rule — only run when the structural rules above pass,
        // so a clearly weak password never burns an external HTTP call.
        RuleFor(x => x.Password)
            .MustAsync(NotBePwnedAsync)
            .WithMessage(AuthErrorMessages.PasswordPwned)
            .When(_ => StructuralRulesPass(_));
    }

    public async Task<PasswordValidationResult> ValidatePasswordAsync(PasswordValidationContext context, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var result = await ValidateAsync(context, ct);

        return result.IsValid
            ? PasswordValidationResult.Success
            : PasswordValidationResult.Failure(result.Errors.Select(e => e.ErrorMessage).ToArray());
    }

    private static bool IsSpecialCharacter(char c) => ValidationLimits.PasswordSpecialCharacters.Contains(c);

    private static bool NotMatchIdentity(PasswordValidationContext ctx)
    {
        if (string.IsNullOrEmpty(ctx.Password))
            return true;

        if (!string.IsNullOrEmpty(ctx.Email) &&
            string.Equals(ctx.Password, ctx.Email, StringComparison.OrdinalIgnoreCase))
            return false;

        if (!string.IsNullOrEmpty(ctx.Username) &&
            string.Equals(ctx.Password, ctx.Username, StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }

    private async Task<bool> NotBePwnedAsync(string password, CancellationToken ct)
    {
        var isPwned = await _pwnedPasswordsClient.IsPwnedAsync(password, ct);
        return !isPwned;
    }

    private static bool StructuralRulesPass(PasswordValidationContext ctx)
    {
        var password = ctx.Password;

        if (string.IsNullOrEmpty(password)) return false;
        if (password.Length < ValidationLimits.PasswordMinLength) return false;
        if (!password.Any(char.IsUpper)) return false;
        if (!password.Any(char.IsLower)) return false;
        if (!password.Any(char.IsDigit)) return false;
        if (!password.Any(IsSpecialCharacter)) return false;
        if (!NotMatchIdentity(ctx)) return false;

        return true;
    }
}
