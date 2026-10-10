using FluentAssertions;
using Lumen.Modules.Identity.Domain.Tokens;
using Lumen.SharedKernel.Constants;

namespace Lumen.Modules.Identity.Tests.Domain;

public sealed class OneTimeTokenPolicyTests
{
    [Fact]
    public void ValidateCreation_NullTokenHash_ThrowsArgumentException()
    {
        var futureExpiry = DateTime.UtcNow.AddHours(1);

        var act = () => OneTimeTokenPolicy.ValidateCreation(null!, futureExpiry);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ValidateCreation_WhitespaceTokenHash_ThrowsArgumentException()
    {
        var futureExpiry = DateTime.UtcNow.AddHours(1);

        var act = () => OneTimeTokenPolicy.ValidateCreation("   ", futureExpiry);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ValidateCreation_ExpiresAtInPast_ThrowsArgumentExceptionWithExpectedMessage()
    {
        var pastExpiry = DateTime.UtcNow.AddSeconds(-1);

        var act = () => OneTimeTokenPolicy.ValidateCreation("valid_hash", pastExpiry);

        act.Should().Throw<ArgumentException>()
            .WithMessage($"*{AuthErrorMessages.ExpiresAtMustBeInFuture}*");
    }

    [Fact]
    public void ValidateCreation_ExpiresAtNow_ThrowsArgumentException()
    {
        var nowExpiry = DateTime.UtcNow;

        var act = () => OneTimeTokenPolicy.ValidateCreation("valid_hash", nowExpiry);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ValidateCreation_ValidHashAndFutureExpiry_DoesNotThrow()
    {
        var futureExpiry = DateTime.UtcNow.AddHours(1);

        var act = () => OneTimeTokenPolicy.ValidateCreation("valid_hash", futureExpiry);

        act.Should().NotThrow();
    }

    [Fact]
    public void IsExpired_ExpiryInPast_ReturnsTrue()
    {
        var pastExpiry = DateTime.UtcNow.AddSeconds(-1);

        OneTimeTokenPolicy.IsExpired(pastExpiry).Should().BeTrue();
    }

    [Fact]
    public void IsExpired_ExpiryAtExactNow_ReturnsTrue()
    {
        var exactNow = DateTime.UtcNow;

        OneTimeTokenPolicy.IsExpired(exactNow).Should().BeTrue();
    }

    [Fact]
    public void IsExpired_ExpiryInFuture_ReturnsFalse()
    {
        var futureExpiry = DateTime.UtcNow.AddHours(1);

        OneTimeTokenPolicy.IsExpired(futureExpiry).Should().BeFalse();
    }

    [Fact]
    public void IsUsed_UsedAtHasValue_ReturnsTrue()
    {
        OneTimeTokenPolicy.IsUsed(DateTime.UtcNow).Should().BeTrue();
    }

    [Fact]
    public void IsUsed_UsedAtIsNull_ReturnsFalse()
    {
        OneTimeTokenPolicy.IsUsed(null).Should().BeFalse();
    }

    [Fact]
    public void IsValid_NotExpiredAndNotUsed_ReturnsTrue()
    {
        var futureExpiry = DateTime.UtcNow.AddHours(1);

        OneTimeTokenPolicy.IsValid(futureExpiry, null).Should().BeTrue();
    }

    [Fact]
    public void IsValid_Expired_ReturnsFalse()
    {
        var pastExpiry = DateTime.UtcNow.AddSeconds(-1);

        OneTimeTokenPolicy.IsValid(pastExpiry, null).Should().BeFalse();
    }

    [Fact]
    public void IsValid_Used_ReturnsFalse()
    {
        var futureExpiry = DateTime.UtcNow.AddHours(1);

        OneTimeTokenPolicy.IsValid(futureExpiry, DateTime.UtcNow).Should().BeFalse();
    }

    [Fact]
    public void IsValid_ExpiredAndUsed_ReturnsFalse()
    {
        var pastExpiry = DateTime.UtcNow.AddSeconds(-1);

        OneTimeTokenPolicy.IsValid(pastExpiry, DateTime.UtcNow).Should().BeFalse();
    }
}
