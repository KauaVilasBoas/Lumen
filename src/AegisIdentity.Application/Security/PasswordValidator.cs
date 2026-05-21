using AegisIdentity.Domain.Security;
using FluentValidation;

namespace AegisIdentity.Application.Security;

public sealed class PasswordValidator : AbstractValidator<PasswordValidationContext>, IPasswordValidator
{
    public const int MinimumLength = 12;
    public const string SpecialCharacters = "!@#$%^&*()-_=+[]{};:'\",.<>/?\\|`~";

    internal static class Messages
    {
        public const string TooShort = "A senha deve ter no mínimo 12 caracteres.";
        public const string MissingUppercase = "A senha deve conter pelo menos uma letra maiúscula.";
        public const string MissingLowercase = "A senha deve conter pelo menos uma letra minúscula.";
        public const string MissingDigit = "A senha deve conter pelo menos um dígito.";
        public const string MissingSpecial = "A senha deve conter pelo menos um caractere especial.";
        public const string EqualsIdentity = "A senha não pode ser igual ao seu email/username.";
        public const string Pwned = "Esta senha aparece em vazamentos públicos conhecidos. Escolha outra.";
    }

    private readonly IPwnedPasswordsClient _pwnedPasswordsClient;

    public PasswordValidator(IPwnedPasswordsClient pwnedPasswordsClient)
    {
        _pwnedPasswordsClient = pwnedPasswordsClient;

        RuleFor(x => x.Password)
            .Must(p => !string.IsNullOrEmpty(p) && p.Length >= MinimumLength)
            .WithMessage(Messages.TooShort);

        RuleFor(x => x.Password)
            .Must(p => !string.IsNullOrEmpty(p) && p.Any(char.IsUpper))
            .WithMessage(Messages.MissingUppercase);

        RuleFor(x => x.Password)
            .Must(p => !string.IsNullOrEmpty(p) && p.Any(char.IsLower))
            .WithMessage(Messages.MissingLowercase);

        RuleFor(x => x.Password)
            .Must(p => !string.IsNullOrEmpty(p) && p.Any(char.IsDigit))
            .WithMessage(Messages.MissingDigit);

        RuleFor(x => x.Password)
            .Must(p => !string.IsNullOrEmpty(p) && p.Any(IsSpecialCharacter))
            .WithMessage(Messages.MissingSpecial);

        RuleFor(x => x)
            .Must(NotMatchIdentity)
            .WithMessage(Messages.EqualsIdentity);

        // HIBP check is the most expensive rule — only run when the structural rules above pass,
        // so a clearly weak password never burns an external HTTP call.
        RuleFor(x => x.Password)
            .MustAsync(NotBePwnedAsync)
            .WithMessage(Messages.Pwned)
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

    private static bool IsSpecialCharacter(char c) => SpecialCharacters.Contains(c);

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
        if (password.Length < MinimumLength) return false;
        if (!password.Any(char.IsUpper)) return false;
        if (!password.Any(char.IsLower)) return false;
        if (!password.Any(char.IsDigit)) return false;
        if (!password.Any(IsSpecialCharacter)) return false;
        if (!NotMatchIdentity(ctx)) return false;

        return true;
    }
}
