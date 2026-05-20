namespace AegisIdentity.Api.Logging;

/// <summary>
/// Fields that must never appear as structured log arguments. Enforcement is by code review.
/// FORBIDDEN: <c>Log.Information("...{@Request}", loginRequest)</c> — would log raw password.
/// SAFE:      <c>Log.Information("...{Email}", loginRequest.Email)</c>.
/// </summary>
public static class SensitiveDataConvention
{
    public static readonly IReadOnlyList<string> Fields =
    [
        "Password",
        "PasswordHash",
        "Token",
        "AccessToken",
        "RefreshToken",
        "ResetCode",
        "Secret",
    ];
}
