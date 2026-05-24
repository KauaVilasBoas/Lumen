using AegisIdentity.CommandHandlers.Auth.Login;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace AegisIdentity.UnitTests.Application.Auth.Login;

public sealed class LoginRequestValidatorTests
{
    private readonly LoginUserCommandHandler.Validator _validator = new();

    // ── Identifier ────────────────────────────────────────────────────────

    [Fact]
    public async Task ValidateAsync_WithValidIdentifier_PassesIdentifierRule()
    {
        var result = await _validator.TestValidateAsync(ValidCommand());
        result.ShouldNotHaveValidationErrorFor(r => r.Identifier);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ValidateAsync_WhenIdentifierIsEmpty_FailsRequiredRule(string identifier)
    {
        var result = await _validator.TestValidateAsync(ValidCommand() with { Identifier = identifier });
        result.ShouldHaveValidationErrorFor(r => r.Identifier)
            .WithErrorMessage("O campo identificador é obrigatório.");
    }

    // ── Password ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ValidateAsync_WithNonEmptyPassword_PassesPasswordRule()
    {
        var result = await _validator.TestValidateAsync(ValidCommand());
        result.ShouldNotHaveValidationErrorFor(r => r.Password);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ValidateAsync_WhenPasswordIsEmpty_FailsRequiredRule(string password)
    {
        var result = await _validator.TestValidateAsync(ValidCommand() with { Password = password });
        result.ShouldHaveValidationErrorFor(r => r.Password)
            .WithErrorMessage("O campo senha é obrigatório.");
    }

    // ── Full valid command ────────────────────────────────────────────────

    [Fact]
    public async Task ValidateAsync_WithFullyValidCommand_HasNoErrors()
    {
        var result = await _validator.TestValidateAsync(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static LoginUserCommandHandler.Command ValidCommand() =>
        new("alice@example.com", "Str0ng!Passw0rd-2026", "127.0.0.1");
}
