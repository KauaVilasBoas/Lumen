namespace Lumen.Domain.Security;

/// <summary>
/// Outcome of a password policy validation.
/// <see cref="Success"/> is a cached singleton to avoid repeated allocations on the happy path.
/// </summary>
public sealed record PasswordValidationResult(bool IsValid, IReadOnlyList<string> Errors)
{
    public static PasswordValidationResult Success { get; } = new(true, Array.Empty<string>());

    public static PasswordValidationResult Failure(IReadOnlyList<string> errors) => new(false, errors);
}
