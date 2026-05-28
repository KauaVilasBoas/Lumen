namespace AegisIdentity.SharedKernel.Constants;

/// <summary>
/// Well-known token type identifiers used in authentication responses.
/// Centralised here so that the value is not duplicated across the handler,
/// response models, and any consumer that validates the scheme.
/// </summary>
public static class TokenTypes
{
    /// <summary>
    /// Standard OAuth 2.0 bearer token scheme.
    /// Returned in the <c>token_type</c> field of every successful login response.
    /// </summary>
    public const string Bearer = "Bearer";
}
