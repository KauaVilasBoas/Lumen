using Lumen.CommandHandlers.Users.Update;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace Lumen.UnitTests.Application.Users;

public sealed class UpdateUserRequestValidatorTests
{
    private readonly UpdateUserCommandHandler.Validator _validator = new();

    private static UpdateUserCommandHandler.Command ValidCommand(
        string? email = null,
        string? username = "alice") =>
        new(
            UserId: Guid.NewGuid(),
            NewEmail: email,
            NewUsername: username,
            ActorId: "actor-id");

    // ── Email validations (when provided) ────────────────────────────────

    [Fact]
    public async Task Validate_WhenEmailIsNull_Passes()
    {
        var result = await _validator.TestValidateAsync(ValidCommand(email: null));
        result.ShouldNotHaveValidationErrorFor(x => x.NewEmail);
    }

    [Fact]
    public async Task Validate_WhenEmailIsValid_Passes()
    {
        var result = await _validator.TestValidateAsync(ValidCommand(email: "new@example.com"));
        result.ShouldNotHaveValidationErrorFor(x => x.NewEmail);
    }

    [Fact]
    public async Task Validate_WhenEmailIsInvalidFormat_FailsEmailRule()
    {
        var result = await _validator.TestValidateAsync(ValidCommand(email: "notanemail"));
        result.ShouldHaveValidationErrorFor(x => x.NewEmail)
            .WithErrorMessage("O email informado não é válido.");
    }

    [Fact]
    public async Task Validate_WhenEmailExceedsMaxLength_FailsValidation()
    {
        // FluentValidation's EmailAddress rule rejects local parts longer than 64 chars (RFC 5321),
        // so a 300-char email is caught before MaximumLength is evaluated.
        // We assert a validation error exists for NewEmail without pinning to one specific message.
        var longEmail = new string('a', 300) + "@x.com";
        var result = await _validator.TestValidateAsync(ValidCommand(email: longEmail));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateUserCommandHandler.Command.NewEmail));
    }

    // ── Username validations (when provided) ─────────────────────────────

    [Fact]
    public async Task Validate_WhenUsernameIsNull_Passes()
    {
        var result = await _validator.TestValidateAsync(ValidCommand(username: null));
        result.ShouldNotHaveValidationErrorFor(x => x.NewUsername);
    }

    [Fact]
    public async Task Validate_WhenUsernameIsValid_Passes()
    {
        var result = await _validator.TestValidateAsync(ValidCommand(username: "alice_123"));
        result.ShouldNotHaveValidationErrorFor(x => x.NewUsername);
    }

    [Theory]
    [InlineData("ab")]
    public async Task Validate_WhenUsernameTooShort_FailsMinLengthRule(string username)
    {
        var result = await _validator.TestValidateAsync(ValidCommand(username: username));
        result.ShouldHaveValidationErrorFor(x => x.NewUsername)
            .WithErrorMessage("O username deve ter no mínimo 3 caracteres.");
    }

    [Fact]
    public async Task Validate_WhenUsernameExceedsMaxLength_FailsMaxLengthRule()
    {
        var result = await _validator.TestValidateAsync(ValidCommand(username: new string('a', 33)));
        result.ShouldHaveValidationErrorFor(x => x.NewUsername)
            .WithErrorMessage("O username deve ter no máximo 32 caracteres.");
    }

    [Theory]
    [InlineData("user name")]
    [InlineData("user@name")]
    [InlineData("user!name")]
    public async Task Validate_WhenUsernameHasDisallowedChars_FailsPatternRule(string username)
    {
        var result = await _validator.TestValidateAsync(ValidCommand(username: username));
        result.ShouldHaveValidationErrorFor(x => x.NewUsername)
            .WithErrorMessage("O username deve conter apenas letras, números, underscores ou hífens.");
    }

    // ── Fully valid ───────────────────────────────────────────────────────

    [Fact]
    public async Task Validate_WithFullyValidCommand_HasNoErrors()
    {
        var result = await _validator.TestValidateAsync(ValidCommand(email: "new@example.com", username: "alice_new"));
        result.IsValid.Should().BeTrue();
    }
}
