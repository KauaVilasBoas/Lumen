namespace Lumen.Modules.Identity.Domain.Security;

internal sealed record PasswordValidationResult(bool IsValid, IReadOnlyList<string> Errors)
{
    public static PasswordValidationResult Success { get; } = new(true, Array.Empty<string>());

    public static PasswordValidationResult Failure(IReadOnlyList<string> errors) => new(false, errors);
}
