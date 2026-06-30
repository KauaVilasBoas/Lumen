using FluentValidation.TestHelper;
using Lumen.Modules.Identity.Application.Auth.Register;

namespace Lumen.Modules.Identity.Tests.Application;

public sealed class RegisterValidatorTests
{
    private readonly RegisterCommandHandler.Validator _validator = new();

    [Fact]
    public void Validate_EmptyEmail_ProducesError()
    {
        var result = _validator.TestValidate(new RegisterCommand("", "alice", "pass"));
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_InvalidEmail_ProducesError()
    {
        var result = _validator.TestValidate(new RegisterCommand("not-an-email", "alice", "pass"));
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_EmailTooLong_ProducesError()
    {
        var longEmail = new string('a', 252) + "@x.com";
        var result = _validator.TestValidate(new RegisterCommand(longEmail, "alice", "pass"));
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_EmptyUsername_ProducesError()
    {
        var result = _validator.TestValidate(new RegisterCommand("a@b.com", "", "pass"));
        result.ShouldHaveValidationErrorFor(x => x.Username);
    }

    [Fact]
    public void Validate_UsernameTooShort_ProducesError()
    {
        var result = _validator.TestValidate(new RegisterCommand("a@b.com", "ab", "pass"));
        result.ShouldHaveValidationErrorFor(x => x.Username);
    }

    [Fact]
    public void Validate_UsernameTooLong_ProducesError()
    {
        var longUsername = new string('a', 65);
        var result = _validator.TestValidate(new RegisterCommand("a@b.com", longUsername, "pass"));
        result.ShouldHaveValidationErrorFor(x => x.Username);
    }

    [Fact]
    public void Validate_UsernameInvalidChars_ProducesError()
    {
        var result = _validator.TestValidate(new RegisterCommand("a@b.com", "alice!!!", "pass"));
        result.ShouldHaveValidationErrorFor(x => x.Username);
    }

    [Fact]
    public void Validate_EmptyPassword_ProducesError()
    {
        var result = _validator.TestValidate(new RegisterCommand("a@b.com", "alice", ""));
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        var result = _validator.TestValidate(new RegisterCommand("alice@test.com", "alice", "pass"));
        result.ShouldNotHaveAnyValidationErrors();
    }
}
