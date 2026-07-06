using System.Text;
using Lumen.Identity.Infrastructure.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Lumen.Identity.Infrastructure.Security;

/// <summary>
/// Exposes JWT token validation parameters derived from <see cref="IdentityJwtOptions"/>
/// so that ASP.NET Core host projects can configure <c>JwtBearerOptions</c> without
/// depending on the internal <c>JwtService</c> implementation.
/// </summary>
public static class IdentityJwtParametersBuilder
{
    private static readonly TimeSpan ValidationClockSkew = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Builds <see cref="TokenValidationParameters"/> from the supplied <paramref name="options"/>.
    /// </summary>
    public static TokenValidationParameters Build(IdentityJwtOptions options)
    {
        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = options.Issuer,
            ValidateAudience = true,
            ValidAudience = options.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Secret)),
            ClockSkew = ValidationClockSkew,
        };
    }
}
