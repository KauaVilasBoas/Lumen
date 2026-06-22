using Lumen.CommandHandlers.Auth.ResetPassword;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace Lumen.UnitTests.Application.Auth.ResetPassword;

public sealed class ResetPasswordRequestValidatorTests
{
    private readonly ResetPasswordCommandHandler.Validator _validator = new();

    [Fact]
    public async Task ValidateAsync_WithValidCommand_PassesAllRules()
    {
        var result = await _validator.TestValidateAsync(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ValidateAsync_WhenTokenIsEmpty_FailsRequiredRule(string token)
    {
        var result = await _validator.TestValidateAsync(new ResetPasswordCommandHandler.Command(token, "SomePassword1!"));
        result.ShouldHaveValidationErrorFor(r => r.Token)
            .WithErrorMessage("O token é obrigatório.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ValidateAsync_WhenNewPasswordIsEmpty_FailsRequiredRule(string password)
    {
        var result = await _validator.TestValidateAsync(new ResetPasswordCommandHandler.Command("valid-token", password));
        result.ShouldHaveValidationErrorFor(r => r.NewPassword)
            .WithErrorMessage("A nova senha é obrigatória.");
    }

    private static ResetPasswordCommandHandler.Command ValidCommand() =>
        new("valid-reset-token", "Str0ng!Password");
}
