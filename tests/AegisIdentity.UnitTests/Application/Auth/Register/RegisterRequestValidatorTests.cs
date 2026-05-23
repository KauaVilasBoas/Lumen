using AegisIdentity.Application.Auth.Register;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace AegisIdentity.UnitTests.Application.Auth.Register;

public sealed class RegisterRequestValidatorTests
{
    private readonly RegisterRequestValidator _validator = new();

    // ── Email ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task ValidateAsync_WithValidEmail_PassesEmailRule()
    {
        var result = await _validator.TestValidateAsync(ValidRequest());
        result.ShouldNotHaveValidationErrorFor(r => r.Email);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ValidateAsync_WhenEmailIsEmpty_FailsRequiredRule(string email)
    {
        var result = await _validator.TestValidateAsync(ValidRequest() with { Email = email });
        result.ShouldHaveValidationErrorFor(r => r.Email)
            .WithErrorMessage("O campo email é obrigatório.");
    }

    [Fact]
    public async Task ValidateAsync_WhenEmailMissingAtSign_FailsEmailAddressRule()
    {
        var result = await _validator.TestValidateAsync(ValidRequest() with { Email = "notanemail" });
        result.ShouldHaveValidationErrorFor(r => r.Email)
            .WithErrorMessage("O email informado não é válido.");
    }

    [Fact]
    public async Task ValidateAsync_WhenEmailExceeds256Chars_FailsValidation()
    {
        // FluentValidation's EmailAddress rule rejects local parts longer than 64 chars
        // (RFC 5321), so a 300-char email is caught before MaximumLength is evaluated.
        // We assert a validation error exists for Email without pinning to one specific message.
        var email = new string('a', 300) + "@x.com";
        var result = await _validator.TestValidateAsync(ValidRequest() with { Email = email });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterRequest.Email));
    }

    // ── Username ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ValidateAsync_WithValidUsername_PassesUsernameRules()
    {
        var result = await _validator.TestValidateAsync(ValidRequest());
        result.ShouldNotHaveValidationErrorFor(r => r.Username);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ValidateAsync_WhenUsernameIsEmpty_FailsRequiredRule(string username)
    {
        var result = await _validator.TestValidateAsync(ValidRequest() with { Username = username });
        result.ShouldHaveValidationErrorFor(r => r.Username)
            .WithErrorMessage("O campo username é obrigatório.");
    }

    [Fact]
    public async Task ValidateAsync_WhenUsernameTooShort_FailsMinLengthRule()
    {
        var result = await _validator.TestValidateAsync(ValidRequest() with { Username = "ab" });
        result.ShouldHaveValidationErrorFor(r => r.Username)
            .WithErrorMessage("O username deve ter no mínimo 3 caracteres.");
    }

    [Fact]
    public async Task ValidateAsync_WhenUsernameExceeds32Chars_FailsMaxLengthRule()
    {
        var result = await _validator.TestValidateAsync(ValidRequest() with { Username = new string('a', 33) });
        result.ShouldHaveValidationErrorFor(r => r.Username)
            .WithErrorMessage("O username deve ter no máximo 32 caracteres.");
    }

    [Theory]
    [InlineData("user name")]
    [InlineData("user@name")]
    [InlineData("user!name")]
    public async Task ValidateAsync_WhenUsernameHasDisallowedChars_FailsPatternRule(string username)
    {
        var result = await _validator.TestValidateAsync(ValidRequest() with { Username = username });
        result.ShouldHaveValidationErrorFor(r => r.Username)
            .WithErrorMessage("O username deve conter apenas letras, números, underscores ou hífens.");
    }

    [Theory]
    [InlineData("alice")]
    [InlineData("Alice_123")]
    [InlineData("user-name")]
    [InlineData("abc")]
    public async Task ValidateAsync_WithAllowedUsernameChars_PassesPatternRule(string username)
    {
        var result = await _validator.TestValidateAsync(ValidRequest() with { Username = username });
        result.ShouldNotHaveValidationErrorFor(r => r.Username);
    }

    // ── Password ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ValidateAsync_WithNonEmptyPassword_PassesRequiredRule()
    {
        var result = await _validator.TestValidateAsync(ValidRequest());
        result.ShouldNotHaveValidationErrorFor(r => r.Password);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ValidateAsync_WhenPasswordIsEmpty_FailsRequiredRule(string password)
    {
        var result = await _validator.TestValidateAsync(ValidRequest() with { Password = password });
        result.ShouldHaveValidationErrorFor(r => r.Password)
            .WithErrorMessage("O campo senha é obrigatório.");
    }

    // ── Full valid request ────────────────────────────────────────────────

    [Fact]
    public async Task ValidateAsync_WithFullyValidRequest_HasNoErrors()
    {
        var result = await _validator.TestValidateAsync(ValidRequest());
        result.IsValid.Should().BeTrue();
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static RegisterRequest ValidRequest() =>
        new("alice@example.com", "alice_123", "StrongP@ssw0rd!");
}
