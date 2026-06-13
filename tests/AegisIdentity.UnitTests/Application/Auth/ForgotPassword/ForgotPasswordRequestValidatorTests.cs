using AegisIdentity.CommandHandlers.Auth.ForgotPassword;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace AegisIdentity.UnitTests.Application.Auth.ForgotPassword;

public sealed class ForgotPasswordRequestValidatorTests
{
    private readonly ForgotPasswordCommandHandler.Validator _validator = new();

    // ── Email ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task ValidateAsync_WithValidEmail_PassesAllRules()
    {
        var result = await _validator.TestValidateAsync(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ValidateAsync_WhenEmailIsEmpty_FailsRequiredRule(string email)
    {
        var result = await _validator.TestValidateAsync(new ForgotPasswordCommandHandler.Command(email));
        result.ShouldHaveValidationErrorFor(r => r.Email)
            .WithErrorMessage("O campo email é obrigatório.");
    }

    [Fact]
    public async Task ValidateAsync_WhenEmailMissingAtSign_FailsEmailAddressRule()
    {
        var result = await _validator.TestValidateAsync(new ForgotPasswordCommandHandler.Command("notanemail"));
        result.ShouldHaveValidationErrorFor(r => r.Email)
            .WithErrorMessage("O email informado não é válido.");
    }

    [Fact]
    public async Task ValidateAsync_WhenEmailExceeds256Chars_FailsValidation()
    {
        var email = new string('a', 300) + "@x.com";
        var result = await _validator.TestValidateAsync(new ForgotPasswordCommandHandler.Command(email));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ForgotPasswordCommandHandler.Command.Email));
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static ForgotPasswordCommandHandler.Command ValidCommand() =>
        new("alice@example.com");
}
