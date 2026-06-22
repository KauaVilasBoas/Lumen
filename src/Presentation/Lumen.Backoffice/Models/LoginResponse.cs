namespace Lumen.Backoffice.Models;

/// <summary>
/// JSON response from <c>POST /api/auth/login</c>.
/// Mirrors <c>LoginUserCommandHandler.Result.Success</c> from the Api assembly.
/// Declared locally so the Backoffice does not reference Application assemblies.
/// </summary>
public sealed record LoginResponse(string AccessToken, string RefreshToken, int ExpiresIn);