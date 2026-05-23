using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AegisIdentity.Domain.Users;
using AegisIdentity.Infrastructure.Configuration;
using AegisIdentity.Infrastructure.Security;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AegisIdentity.UnitTests.Infrastructure.Security;

public sealed class JwtServiceTests
{
    private const string Secret = "super-secret-key-that-is-at-least-32-chars!!";
    private const string Issuer = "aegis-tests";
    private const string Audience = "aegis-client";
    private const int ExpirationMinutes = 15;

    private readonly JwtService _service = new(Options.Create(new JwtOptions
    {
        Secret = Secret,
        Issuer = Issuer,
        Audience = Audience,
        ExpirationMinutes = ExpirationMinutes,
        RefreshExpirationDays = 7,
    }));

    // ── GenerateAccessToken ───────────────────────────────────────────────

    [Fact]
    public void GenerateAccessToken_ReturnsValidJwt()
    {
        var user = ActiveUser();

        var token = _service.GenerateAccessToken(user);

        token.Should().NotBeNullOrWhiteSpace();
        var parts = token.Split('.');
        parts.Should().HaveCount(3, "a JWT has three dot-separated parts");
    }

    [Fact]
    public void GenerateAccessToken_TokenValidatesWithCorrectSecret()
    {
        var user = ActiveUser();

        var token = _service.GenerateAccessToken(user);

        var principal = ValidateToken(token);
        principal.Should().NotBeNull();
    }

    [Fact]
    public void GenerateAccessToken_ContainsSubClaim()
    {
        var user = ActiveUser();

        var token = _service.GenerateAccessToken(user);

        // JwtSecurityTokenHandler maps "sub" → ClaimTypes.NameIdentifier during validation.
        var principal = ValidateToken(token)!;
        principal.FindFirstValue(ClaimTypes.NameIdentifier).Should().Be(user.Id);
    }

    [Fact]
    public void GenerateAccessToken_ContainsEmailClaim()
    {
        var user = ActiveUser();

        var token = _service.GenerateAccessToken(user);

        // JwtSecurityTokenHandler maps "email" → ClaimTypes.Email during validation.
        var principal = ValidateToken(token)!;
        principal.FindFirstValue(ClaimTypes.Email).Should().Be(user.Email);
    }

    [Fact]
    public void GenerateAccessToken_ContainsUsernameClaim()
    {
        var user = ActiveUser();

        var token = _service.GenerateAccessToken(user);

        var principal = ValidateToken(token)!;
        principal.FindFirstValue("username").Should().Be(user.Username);
    }

    [Fact]
    public void GenerateAccessToken_ContainsRoleClaims()
    {
        var user = ActiveUser();

        var token = _service.GenerateAccessToken(user);

        var principal = ValidateToken(token)!;
        var roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        roles.Should().Contain("user");
    }

    [Fact]
    public void GenerateAccessToken_ExpiresAfterConfiguredMinutes()
    {
        var user = ActiveUser();

        var token = _service.GenerateAccessToken(user);

        var handler = new JwtSecurityTokenHandler();
        var parsed = handler.ReadJwtToken(token);
        var expectedExpiry = DateTime.UtcNow.AddMinutes(ExpirationMinutes);

        parsed.ValidTo.Should().BeCloseTo(expectedExpiry, TimeSpan.FromSeconds(5));
    }

    // ── GenerateRefreshTokenValue ─────────────────────────────────────────

    [Fact]
    public void GenerateRefreshTokenValue_ReturnsNonEmptyString()
    {
        var value = _service.GenerateRefreshTokenValue();
        value.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void GenerateRefreshTokenValue_ReturnsDifferentValueEachCall()
    {
        var first = _service.GenerateRefreshTokenValue();
        var second = _service.GenerateRefreshTokenValue();

        first.Should().NotBe(second);
    }

    [Fact]
    public void GenerateRefreshTokenValue_IsUrlSafe()
    {
        for (var i = 0; i < 20; i++)
        {
            var value = _service.GenerateRefreshTokenValue();
            value.Should().NotContain("+", "Base64Url replaces + with -");
            value.Should().NotContain("/", "Base64Url replaces / with _");
            value.Should().NotContain("=", "Base64Url removes padding");
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static User ActiveUser()
    {
        var user = User.Create("alice@example.com", "alice", "$2a$12$fakehash");
        user.IsActive = true;
        return user;
    }

    private ClaimsPrincipal? ValidateToken(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = Issuer,
            ValidateAudience = true,
            ValidAudience = Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
        };

        return handler.ValidateToken(token, parameters, out _);
    }
}
