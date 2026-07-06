using System.Security.Claims;
using Lumen.Identity.Domain.Users;

namespace Lumen.Identity.Domain.Security;

public interface IJwtService
{
    string GenerateAccessToken(User user);

    string GenerateRefreshTokenValue();

    int AccessTokenExpiresIn { get; }

    ClaimsPrincipal? ValidateToken(string token);
}
