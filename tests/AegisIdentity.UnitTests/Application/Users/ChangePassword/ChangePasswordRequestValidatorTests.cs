using AegisIdentity.CommandHandlers.Users.ChangePassword;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace AegisIdentity.UnitTests.Application.Users.ChangePassword;

public sealed class ChangePasswordRequestValidatorTests
{
    private readonly ChangePasswordCommandHandler.Validator _validator = new();

    private static readonly Guid AnyUserId = Guid.NewGuid();

    [Fact]
    public async Task ValidateAsync_WithValidCommand_PassesAllRules()
    {
        var result = await _validator.TestValidateAsync(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ValidateAsync_WhenCurrentPasswordIsEmpty_FailsRequiredRule(string password)
    {
        var result = await _validator.TestValidateAsync(
            new ChangePasswordCommandHandler.Command(AnyUserId, password, "NewPass1!strong"));

        result.ShouldHaveValidationErrorFor(r => r.CurrentPassword)
            .WithErrorMessage("A senha atual é obrigatória.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ValidateAsync_WhenNewPasswordIsEmpty_FailsRequiredRule(string password)
    {
        var result = await _validator.TestValidateAsync(
            new ChangePasswordCommandHandler.Command(AnyUserId, "CurrentPass1!", password));

        result.ShouldHaveValidationErrorFor(r => r.NewPassword)
            .WithErrorMessage("A nova senha é obrigatória.");
    }

    private static ChangePasswordCommandHandler.Command ValidCommand() =>
        new(AnyUserId, "CurrentStr0ng!Pass", "NewStr0ng!Pass");
}
