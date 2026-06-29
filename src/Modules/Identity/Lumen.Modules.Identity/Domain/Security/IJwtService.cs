using System.Security.Claims;
using Lumen.Modules.Identity.Domain.Users;

namespace Lumen.Modules.Identity.Domain.Security;

internal interface IJwtService
{
    string GenerateAccessToken(User user);

    string GenerateRefreshTokenValue();

    int AccessTokenExpiresIn { get; }

    ClaimsPrincipal? ValidateToken(string token);
}
