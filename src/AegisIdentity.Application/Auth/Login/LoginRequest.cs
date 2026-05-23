namespace AegisIdentity.Application.Auth.Login;

/// <summary>
/// Credential payload for the login endpoint.
/// <para>
/// <see cref="Identifier"/> accepts either an email address or a username.
/// The presence of '@' is used to discriminate between the two at the use-case level.
/// </para>
/// </summary>
public sealed record LoginRequest(string Identifier, string Password);
