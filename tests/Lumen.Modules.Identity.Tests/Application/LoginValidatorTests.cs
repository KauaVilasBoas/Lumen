using FluentAssertions;
using FluentValidation.TestHelper;
using Lumen.Modules.Identity.Application.Auth.Login;

namespace Lumen.Modules.Identity.Tests.Application;

public sealed class LoginValidatorTests
{
    private readonly LoginCommandHandler.Validator _validator = new();

    [Fact]
    public void Validate_EmptyIdentifier_ProducesError()
    {
        var result = _validator.TestValidate(new LoginCommand("", "pass", "ip"));
        result.ShouldHaveValidationErrorFor(x => x.Identifier);
    }

    [Fact]
    public void Validate_EmptyPassword_ProducesError()
    {
        var result = _validator.TestValidate(new LoginCommand("user", "", "ip"));
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        var result = _validator.TestValidate(new LoginCommand("user", "pass", "ip"));
        result.ShouldNotHaveAnyValidationErrors();
    }
}
