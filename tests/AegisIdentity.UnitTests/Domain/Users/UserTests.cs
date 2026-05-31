using AegisIdentity.Domain.Users;
using FluentAssertions;

namespace AegisIdentity.UnitTests.Domain.Users;

public sealed class UserTests
{
    [Fact]
    public void Create_WithValidArgs_ReturnsInactiveUser()
    {
        var user = User.Create("Alice@Example.COM", "alice", "hash");

        user.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Create_NormalisesEmail_ToLowercaseAndTrimmed()
    {
        var user = User.Create("  Alice@Example.COM  ", "alice", "hash");

        user.Email.Should().Be("alice@example.com");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankEmail_ThrowsArgumentException(string email)
    {
        var act = () => User.Create(email, "alice", "hash");

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankUsername_ThrowsArgumentException(string username)
    {
        var act = () => User.Create("alice@example.com", username, "hash");

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankPasswordHash_ThrowsArgumentException(string hash)
    {
        var act = () => User.Create("alice@example.com", "alice", hash);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_SetsCreatedAtAndUpdatedAt_ToUtcNow()
    {
        var before = DateTime.UtcNow;
        var user = User.Create("alice@example.com", "alice", "hash");
        var after = DateTime.UtcNow;

        user.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        user.UpdatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Theory]
    [InlineData("Alice@Example.COM", "alice@example.com")]
    [InlineData("  BOB@DOMAIN.IO  ", "bob@domain.io")]
    [InlineData("already@lowercase.com", "already@lowercase.com")]
    public void NormalizeEmail_ReturnsTrimmedLowercase(string input, string expected)
    {
        User.NormalizeEmail(input).Should().Be(expected);
    }

    [Fact]
    public void RecordFailedLogin_BelowThreshold_DoesNotLockAccount()
    {
        var user = User.Create("alice@example.com", "alice", "hash");

        user.RecordFailedLogin(lockoutThreshold: 5, lockoutDuration: TimeSpan.FromMinutes(15));

        user.FailedLoginAttempts.Should().Be(1);
        user.LockedUntil.Should().BeNull();
        user.IsLockedOut().Should().BeFalse();
    }

    [Fact]
    public void RecordFailedLogin_AtThreshold_LocksAccount()
    {
        var user = User.Create("alice@example.com", "alice", "hash");

        for (var i = 0; i < 5; i++)
            user.RecordFailedLogin(lockoutThreshold: 5, lockoutDuration: TimeSpan.FromMinutes(15));

        user.LockedUntil.Should().NotBeNull();
        user.IsLockedOut().Should().BeTrue();
    }

    [Fact]
    public void RecordFailedLogin_UpdatesUpdatedAt()
    {
        var user = User.Create("alice@example.com", "alice", "hash");
        var before = DateTime.UtcNow;

        user.RecordFailedLogin(lockoutThreshold: 5, lockoutDuration: TimeSpan.FromMinutes(15));

        user.UpdatedAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void Unlock_ResetsCounterAndClearsLockedUntil()
    {
        var user = User.Create("alice@example.com", "alice", "hash");

        for (var i = 0; i < 5; i++)
            user.RecordFailedLogin(lockoutThreshold: 5, lockoutDuration: TimeSpan.FromMinutes(15));

        user.Unlock();

        user.FailedLoginAttempts.Should().Be(0);
        user.LockedUntil.Should().BeNull();
        user.IsLockedOut().Should().BeFalse();
    }

    [Fact]
    public void IsLockedOut_ReturnsFalse_WhenLockedUntilIsNull()
    {
        var user = User.Create("alice@example.com", "alice", "hash");

        user.IsLockedOut().Should().BeFalse();
    }

    [Fact]
    public void IsLockedOut_ReturnsFalse_WhenLockoutHasExpired()
    {
        var user = User.Create("alice@example.com", "alice", "hash");
        user.Unlock();

        typeof(User)
            .GetProperty(nameof(User.LockedUntil))!
            .SetValue(user, DateTime.UtcNow.AddSeconds(-1));

        user.IsLockedOut().Should().BeFalse();
    }

}
