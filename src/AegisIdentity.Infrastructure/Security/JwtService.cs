using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AegisIdentity.Application.Security;
using AegisIdentity.Domain.Users;
using AegisIdentity.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AegisIdentity.Infrastructure.Security;

public sealed class JwtService : IJwtService
{
    // 32 bytes of entropy for the opaque refresh token value — 256 bits of randomness
    // make it infeasible to enumerate tokens even with an unlimited request rate.
    private const int RefreshTokenByteLength = 32;

    private readonly JwtOptions _options;

    public JwtService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public string GenerateAccessToken(User user)
    {
        var keyBytes = Encoding.UTF8.GetBytes(_options.Secret);
        var signingKey = new SymmetricSecurityKey(keyBytes);
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var claims = BuildClaims(user);

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_options.ExpirationMinutes),
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            SigningCredentials = credentials,
        };

        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateToken(descriptor);
        return handler.WriteToken(token);
    }

    public int AccessTokenExpiresIn => _options.ExpirationMinutes * 60;

    public string GenerateRefreshTokenValue()
    {
        var bytes = RandomNumberGenerator.GetBytes(RefreshTokenByteLength);
        // Base64Url encoding is URL-safe and avoids padding characters.
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static IEnumerable<Claim> BuildClaims(User user)
    {
        yield return new Claim(JwtRegisteredClaimNames.Sub, user.Id);
        yield return new Claim(JwtRegisteredClaimNames.Email, user.Email);
        yield return new Claim("username", user.Username);
        yield return new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString());

        foreach (var role in user.Roles)
            yield return new Claim(ClaimTypes.Role, role);
    }
}
