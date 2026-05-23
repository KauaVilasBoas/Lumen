namespace AegisIdentity.Application.Auth.Login;

/// <summary>
/// Tokens returned to the client on a successful login.
/// </summary>
/// <param name="AccessToken">Signed JWT access token. Short-lived.</param>
/// <param name="RefreshToken">Opaque refresh token. Long-lived, single-use.</param>
/// <param name="ExpiresIn">Access token lifetime in seconds.</param>
public sealed record LoginResponse(string AccessToken, string RefreshToken, int ExpiresIn);
