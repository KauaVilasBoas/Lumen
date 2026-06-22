using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Lumen.Domain.Security;
using Lumen.Domain.Users;
using Lumen.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Lumen.Infrastructure.Security;

public sealed class JwtService : IJwtService
{
    // 32 bytes of entropy for the opaque refresh token value — 256 bits of randomness
    // make it infeasible to enumerate tokens even with an unlimited request rate.
    private const int RefreshTokenByteLength = 32;

    // A small clock skew tolerates minor time drift between token issuer and validator
    // without accepting tokens that expired minutes ago.
    private static readonly TimeSpan ValidationClockSkew = TimeSpan.FromSeconds(30);

    private readonly JwtOptions _options;

    public JwtService(IOptions<JwtOptions> options)
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
        // Base64Url encoding is URL-safe and avoids padding characters.
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    /// <inheritdoc />
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
            // Return null for any token that is malformed, expired, or fails
            // signature / issuer / audience checks — callers treat null as auth failure.
            return null;
        }
    }

    /// <summary>
    /// Builds the <see cref="TokenValidationParameters"/> from <paramref name="options"/>.
    /// Exposed as <c>internal static</c> so <see cref="SecurityServiceExtensions"/> can
    /// reuse the same parameters when configuring the JwtBearer middleware, keeping the
    /// validation logic in a single authoritative place inside Infrastructure.
    /// </summary>
    internal static TokenValidationParameters BuildValidationParameters(JwtOptions options)
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
