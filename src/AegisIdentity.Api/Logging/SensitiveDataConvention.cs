namespace AegisIdentity.Api.Logging;

/// <summary>
/// Documents the sensitive fields that MUST NOT appear as structured log arguments anywhere
/// in the codebase.
/// <para>
/// Enforcement is by convention and code review. A destructuring policy or log-sink filter
/// was deliberately not implemented at this stage (SETUP-04) because no code paths that
/// handle these fields exist yet. The defensive infrastructure (e.g. a custom
/// <c>IDestructuringPolicy</c> or a sink wrapper) should be added in the security hardening
/// card once the relevant use cases are in place.
/// </para>
/// <para>
/// Rule: never pass an object containing any of the fields listed in
/// <see cref="Fields"/> as a structured log argument (i.e. as a <c>{@Obj}</c> placeholder
/// or a named parameter whose value is one of these types).
/// </para>
/// </summary>
/// <example>
/// The following is FORBIDDEN — it would log the raw password:
/// <code>
/// Log.Information("User login attempt {@Request}", loginRequest); // loginRequest.Password in plain text!
/// </code>
/// The following is SAFE:
/// <code>
/// Log.Information("User login attempt for {Email}", loginRequest.Email);
/// </code>
/// </example>
public static class SensitiveDataConvention
{
    /// <summary>
    /// Property and field names that must never be emitted as structured log values.
    /// </summary>
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
