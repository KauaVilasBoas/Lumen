using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Lumen.Identity.Domain.Security;
using Lumen.Identity.Domain.Users;
using Lumen.Identity.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Lumen.Identity.Infrastructure.Security;

internal sealed class JwtService : IJwtService
{
    private const int RefreshTokenByteLength = 32;

    private static readonly TimeSpan ValidationClockSkew = TimeSpan.FromSeconds(30);

    private readonly IdentityJwtOptions _options;

    public JwtService(IOptions<IdentityJwtOptions> options)
    {
        _options = options.Value;
    }

    public string GenerateAccessToken(User user)
    {
        var signingKey = BuildSigningKey(_options.Secret);
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
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var parameters = BuildValidationParameters(_options);

        try
        {
            return handler.ValidateToken(token, parameters, out _);
        }
        catch (Exception ex) when (ex is SecurityTokenException or ArgumentException)
        {
            return null;
        }
    }

    public static TokenValidationParameters BuildValidationParameters(IdentityJwtOptions options)
    {
        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = options.Issuer,
            ValidateAudience = true,
            ValidAudience = options.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = BuildSigningKey(options.Secret),
            ClockSkew = ValidationClockSkew,
        };
    }

    private static SymmetricSecurityKey BuildSigningKey(string secret)
        => new(Encoding.UTF8.GetBytes(secret));

    private static IEnumerable<Claim> BuildClaims(User user)
    {
        yield return new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString());
        yield return new Claim(JwtRegisteredClaimNames.Email, user.Email);
        yield return new Claim("username", user.Username);
        yield return new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString());
    }
}
