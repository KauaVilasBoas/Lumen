using AegisIdentity.Domain.Tokens;
using FluentAssertions;

namespace AegisIdentity.UnitTests.Domain.Tokens;

public sealed class PasswordResetTokenTests
{
    private const string UserId = "507f1f77bcf86cd799439011";
    private const string Hash = "abc123hash";

    // ─── Factory ──────────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidArgs_ReturnsUnusedToken()
    {
        var expiresAt = DateTime.UtcNow.AddHours(1);

        var token = PasswordResetToken.Create(UserId, Hash, expiresAt);

        token.UserId.Should().Be(UserId);
        token.TokenHash.Should().Be(Hash);
        token.ExpiresAt.Should().Be(expiresAt);
        token.UsedAt.Should().BeNull();
    }

    [Fact]
    public void Create_SetsCreatedAt_ToUtcNow()
    {
        var before = DateTime.UtcNow;
        var token = PasswordResetToken.Create(UserId, Hash, DateTime.UtcNow.AddHours(1));
        var after = DateTime.UtcNow;

        token.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankUserId_ThrowsArgumentException(string userId)
    {
        var act = () => PasswordResetToken.Create(userId, Hash, DateTime.UtcNow.AddHours(1));

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankTokenHash_ThrowsArgumentException(string hash)
    {
        var act = () => PasswordResetToken.Create(UserId, hash, DateTime.UtcNow.AddHours(1));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithPastExpiresAt_ThrowsArgumentException()
    {
        var act = () => PasswordResetToken.Create(UserId, Hash, DateTime.UtcNow.AddSeconds(-1));

        act.Should().Throw<ArgumentException>().WithMessage("*ExpiresAt*");
    }

    // ─── IsExpired ────────────────────────────────────────────────────────────

    [Fact]
    public void IsExpired_ReturnsFalse_WhenExpiresAtIsInFuture()
    {
        var token = PasswordResetToken.Create(UserId, Hash, DateTime.UtcNow.AddHours(1));

        token.IsExpired().Should().BeFalse();
    }

    [Fact]
    public void IsExpired_ReturnsTrue_WhenExpiresAtIsInPast()
    {
        var token = BuildExpiredToken();

        token.IsExpired().Should().BeTrue();
    }

    // ─── IsUsed ───────────────────────────────────────────────────────────────

    [Fact]
    public void IsUsed_ReturnsFalse_WhenTokenIsNew()
    {
        var token = PasswordResetToken.Create(UserId, Hash, DateTime.UtcNow.AddHours(1));

        token.IsUsed().Should().BeFalse();
    }

    [Fact]
    public void IsUsed_ReturnsTrue_AfterMarkAsUsed()
    {
        var token = PasswordResetToken.Create(UserId, Hash, DateTime.UtcNow.AddHours(1));

        token.MarkAsUsed();

        token.IsUsed().Should().BeTrue();
        token.UsedAt.Should().NotBeNull();
    }

    // ─── IsValid ──────────────────────────────────────────────────────────────

    [Fact]
    public void IsValid_ReturnsTrue_WhenNotExpiredAndNotUsed()
    {
        var token = PasswordResetToken.Create(UserId, Hash, DateTime.UtcNow.AddHours(1));

        token.IsValid().Should().BeTrue();
    }

    [Fact]
    public void IsValid_ReturnsFalse_WhenUsed()
    {
        var token = PasswordResetToken.Create(UserId, Hash, DateTime.UtcNow.AddHours(1));
        token.MarkAsUsed();

        token.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsValid_ReturnsFalse_WhenExpired()
    {
        var token = BuildExpiredToken();

        token.IsValid().Should().BeFalse();
    }

    // ─── MarkAsUsed ───────────────────────────────────────────────────────────

    [Fact]
    public void MarkAsUsed_SetsUsedAt_ToUtcNow()
    {
        var token = PasswordResetToken.Create(UserId, Hash, DateTime.UtcNow.AddHours(1));
        var before = DateTime.UtcNow;

        token.MarkAsUsed();

        token.UsedAt.Should().BeOnOrAfter(before);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static PasswordResetToken BuildExpiredToken()
    {
        var token = PasswordResetToken.Create(UserId, Hash, DateTime.UtcNow.AddHours(1));
        typeof(PasswordResetToken)
            .GetProperty(nameof(PasswordResetToken.ExpiresAt))!
            .SetValue(token, DateTime.UtcNow.AddSeconds(-1));
        return token;
    }
}
