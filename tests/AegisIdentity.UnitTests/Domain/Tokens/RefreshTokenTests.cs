using AegisIdentity.Domain.Tokens;
using FluentAssertions;

namespace AegisIdentity.UnitTests.Domain.Tokens;

public sealed class RefreshTokenTests
{
    private const string UserId = "507f1f77bcf86cd799439011";
    private const string Hash = "abc123hash";
    private const string Ip = "127.0.0.1";

    [Fact]
    public void Create_WithValidArgs_ReturnsActiveToken()
    {
        var expiresAt = DateTime.UtcNow.AddHours(1);

        var token = RefreshToken.Create(UserId, Hash, expiresAt, Ip);

        token.UserId.Should().Be(UserId);
        token.TokenHash.Should().Be(Hash);
        token.ExpiresAt.Should().Be(expiresAt);
        token.CreatedByIp.Should().Be(Ip);
        token.RevokedAt.Should().BeNull();
        token.ReplacedByTokenHash.Should().BeNull();
    }

    [Fact]
    public void Create_SetsCreatedAt_ToUtcNow()
    {
        var before = DateTime.UtcNow;
        var token = RefreshToken.Create(UserId, Hash, DateTime.UtcNow.AddHours(1), Ip);
        var after = DateTime.UtcNow;

        token.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankUserId_ThrowsArgumentException(string userId)
    {
        var act = () => RefreshToken.Create(userId, Hash, DateTime.UtcNow.AddHours(1), Ip);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankTokenHash_ThrowsArgumentException(string hash)
    {
        var act = () => RefreshToken.Create(UserId, hash, DateTime.UtcNow.AddHours(1), Ip);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankCreatedByIp_ThrowsArgumentException(string ip)
    {
        var act = () => RefreshToken.Create(UserId, Hash, DateTime.UtcNow.AddHours(1), ip);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithPastExpiresAt_ThrowsArgumentException()
    {
        var act = () => RefreshToken.Create(UserId, Hash, DateTime.UtcNow.AddSeconds(-1), Ip);

        act.Should().Throw<ArgumentException>().WithMessage("*ExpiresAt*");
    }

    [Fact]
    public void IsExpired_ReturnsFalse_WhenExpiresAtIsInFuture()
    {
        var token = RefreshToken.Create(UserId, Hash, DateTime.UtcNow.AddHours(1), Ip);

        token.IsExpired().Should().BeFalse();
    }

    [Fact]
    public void IsExpired_ReturnsTrue_WhenExpiresAtIsInPast()
    {
        var token = BuildExpiredToken();

        token.IsExpired().Should().BeTrue();
    }

    [Fact]
    public void IsRevoked_ReturnsFalse_WhenTokenIsNew()
    {
        var token = RefreshToken.Create(UserId, Hash, DateTime.UtcNow.AddHours(1), Ip);

        token.IsRevoked().Should().BeFalse();
    }

    [Fact]
    public void IsRevoked_ReturnsTrue_AfterRevoke()
    {
        var token = RefreshToken.Create(UserId, Hash, DateTime.UtcNow.AddHours(1), Ip);

        token.Revoke();

        token.IsRevoked().Should().BeTrue();
        token.RevokedAt.Should().NotBeNull();
    }

    [Fact]
    public void IsActive_ReturnsTrue_WhenNotExpiredAndNotRevoked()
    {
        var token = RefreshToken.Create(UserId, Hash, DateTime.UtcNow.AddHours(1), Ip);

        token.IsActive().Should().BeTrue();
    }

    [Fact]
    public void IsActive_ReturnsFalse_WhenRevoked()
    {
        var token = RefreshToken.Create(UserId, Hash, DateTime.UtcNow.AddHours(1), Ip);
        token.Revoke();

        token.IsActive().Should().BeFalse();
    }

    [Fact]
    public void IsActive_ReturnsFalse_WhenExpired()
    {
        var token = BuildExpiredToken();

        token.IsActive().Should().BeFalse();
    }

    [Fact]
    public void Revoke_WithReplacedByHash_SetsReplacedByTokenHash()
    {
        var token = RefreshToken.Create(UserId, Hash, DateTime.UtcNow.AddHours(1), Ip);

        token.Revoke(replacedByTokenHash: "newHash");

        token.ReplacedByTokenHash.Should().Be("newHash");
        token.RevokedAt.Should().NotBeNull();
    }

    [Fact]
    public void Revoke_WithoutReplacedByHash_LeavesReplacedByTokenHashNull()
    {
        var token = RefreshToken.Create(UserId, Hash, DateTime.UtcNow.AddHours(1), Ip);

        token.Revoke();

        token.ReplacedByTokenHash.Should().BeNull();
        token.RevokedAt.Should().NotBeNull();
    }

    private static RefreshToken BuildExpiredToken()
    {
        var token = RefreshToken.Create(UserId, Hash, DateTime.UtcNow.AddHours(1), Ip);
        typeof(RefreshToken)
            .GetProperty(nameof(RefreshToken.ExpiresAt))!
            .SetValue(token, DateTime.UtcNow.AddSeconds(-1));
        return token;
    }
}
