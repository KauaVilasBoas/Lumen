using Lumen.Domain.Tokens;
using FluentAssertions;

namespace Lumen.UnitTests.Domain.Tokens;

public sealed class EmailConfirmationTokenTests
{
    private static readonly Guid UserId = Guid.Parse("507f1f77-bcf8-6cd7-9943-9011aabbccdd");
    private const string Hash = "abc123hash";

    [Fact]
    public void Create_WithValidArgs_ReturnsUnusedToken()
    {
        var expiresAt = DateTime.UtcNow.AddHours(24);

        var token = EmailConfirmationToken.Create(UserId, Hash, expiresAt);

        token.UserId.Should().Be(UserId);
        token.TokenHash.Should().Be(Hash);
        token.ExpiresAt.Should().Be(expiresAt);
        token.UsedAt.Should().BeNull();
    }

    [Fact]
    public void Create_SetsCreatedAt_ToUtcNow()
    {
        var before = DateTime.UtcNow;
        var token = EmailConfirmationToken.Create(UserId, Hash, DateTime.UtcNow.AddHours(24));
        var after = DateTime.UtcNow;

        token.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankTokenHash_ThrowsArgumentException(string hash)
    {
        var act = () => EmailConfirmationToken.Create(UserId, hash, DateTime.UtcNow.AddHours(24));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithPastExpiresAt_ThrowsArgumentException()
    {
        var act = () => EmailConfirmationToken.Create(UserId, Hash, DateTime.UtcNow.AddSeconds(-1));

        act.Should().Throw<ArgumentException>().WithMessage("*ExpiresAt*");
    }

    [Fact]
    public void IsExpired_ReturnsFalse_WhenExpiresAtIsInFuture()
    {
        var token = EmailConfirmationToken.Create(UserId, Hash, DateTime.UtcNow.AddHours(24));

        token.IsExpired().Should().BeFalse();
    }

    [Fact]
    public void IsExpired_ReturnsTrue_WhenExpiresAtIsInPast()
    {
        var token = BuildExpiredToken();

        token.IsExpired().Should().BeTrue();
    }

    [Fact]
    public void IsUsed_ReturnsFalse_WhenTokenIsNew()
    {
        var token = EmailConfirmationToken.Create(UserId, Hash, DateTime.UtcNow.AddHours(24));

        token.IsUsed().Should().BeFalse();
    }

    [Fact]
    public void IsUsed_ReturnsTrue_AfterMarkAsUsed()
    {
        var token = EmailConfirmationToken.Create(UserId, Hash, DateTime.UtcNow.AddHours(24));

        token.MarkAsUsed();

        token.IsUsed().Should().BeTrue();
        token.UsedAt.Should().NotBeNull();
    }

    [Fact]
    public void IsValid_ReturnsTrue_WhenNotExpiredAndNotUsed()
    {
        var token = EmailConfirmationToken.Create(UserId, Hash, DateTime.UtcNow.AddHours(24));

        token.IsValid().Should().BeTrue();
    }

    [Fact]
    public void IsValid_ReturnsFalse_WhenUsed()
    {
        var token = EmailConfirmationToken.Create(UserId, Hash, DateTime.UtcNow.AddHours(24));
        token.MarkAsUsed();

        token.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsValid_ReturnsFalse_WhenExpired()
    {
        var token = BuildExpiredToken();

        token.IsValid().Should().BeFalse();
    }

    [Fact]
    public void MarkAsUsed_SetsUsedAt_ToUtcNow()
    {
        var token = EmailConfirmationToken.Create(UserId, Hash, DateTime.UtcNow.AddHours(24));
        var before = DateTime.UtcNow;

        token.MarkAsUsed();

        token.UsedAt.Should().BeOnOrAfter(before);
    }

    private static EmailConfirmationToken BuildExpiredToken()
    {
        var token = EmailConfirmationToken.Create(UserId, Hash, DateTime.UtcNow.AddHours(24));
        typeof(EmailConfirmationToken)
            .GetProperty(nameof(EmailConfirmationToken.ExpiresAt))!
            .SetValue(token, DateTime.UtcNow.AddSeconds(-1));
        return token;
    }
}
