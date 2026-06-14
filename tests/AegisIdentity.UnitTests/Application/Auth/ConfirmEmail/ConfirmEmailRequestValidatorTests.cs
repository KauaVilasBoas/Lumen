using AegisIdentity.CommandHandlers.Auth.ConfirmEmail;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace AegisIdentity.UnitTests.Application.Auth.ConfirmEmail;

public sealed class ConfirmEmailRequestValidatorTests
{
    private readonly ConfirmEmailCommandHandler.Validator _validator = new();

    [Fact]
    public async Task ValidateAsync_WithValidToken_PassesAllRules()
    {
        var result = await _validator.TestValidateAsync(new ConfirmEmailCommandHandler.Command("valid-token-value"));
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ValidateAsync_WhenTokenIsEmpty_FailsRequiredRule(string token)
    {
        var result = await _validator.TestValidateAsync(new ConfirmEmailCommandHandler.Command(token));
        result.ShouldHaveValidationErrorFor(r => r.Token)
            .WithErrorMessage("O token é obrigatório.");
    }
}
