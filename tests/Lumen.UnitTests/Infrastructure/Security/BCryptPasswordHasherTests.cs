using AegisIdentity.Infrastructure.Security;
using FluentAssertions;

namespace AegisIdentity.UnitTests.Infrastructure.Security;

public sealed class BCryptPasswordHasherTests
{
    private readonly BCryptPasswordHasher _hasher = new();

    [Fact]
    public void Hash_ReturnsNonEmptyString()
    {
        var hash = _hasher.Hash("Str0ng!Password#99");
        hash.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Hash_ReturnsDifferentHashForSamePlainText_DueToSalt()
    {
        const string password = "Str0ng!Password#99";

        var hash1 = _hasher.Hash(password);
        var hash2 = _hasher.Hash(password);

        // BCrypt salts are randomised per call — two hashes of the same password must differ.
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void Hash_ProducesBCryptFormattedHash()
    {
        var hash = _hasher.Hash("Str0ng!Password#99");
        // BCrypt hashes always start with "$2a$" or "$2b$" followed by the cost factor.
        hash.Should().MatchRegex(@"^\$2[ab]\$\d{2}\$");
    }

    [Fact]
    public void Verify_WithCorrectPassword_ReturnsTrue()
    {
        const string password = "Str0ng!Password#99";
        var hash = _hasher.Hash(password);

        var result = _hasher.Verify(password, hash);

        result.Should().BeTrue();
    }

    [Fact]
    public void Verify_WithWrongPassword_ReturnsFalse()
    {
        var hash = _hasher.Hash("Str0ng!Password#99");

        var result = _hasher.Verify("WrongPassword!1", hash);

        result.Should().BeFalse();
    }

    [Fact]
    public void Hash_ThrowsArgumentException_WhenPasswordIsEmpty()
    {
        var act = () => _hasher.Hash(string.Empty);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Verify_ThrowsArgumentException_WhenPasswordIsEmpty()
    {
        var hash = _hasher.Hash("Str0ng!Password#99");
        var act = () => _hasher.Verify(string.Empty, hash);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Verify_ThrowsArgumentException_WhenHashIsEmpty()
    {
        var act = () => _hasher.Verify("Str0ng!Password#99", string.Empty);
        act.Should().Throw<ArgumentException>();
    }
}
