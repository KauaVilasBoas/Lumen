using FluentAssertions;
using FluentValidation.TestHelper;
using Lumen.Modules.Identity.Application.Auth.ResendConfirmationEmail;
using Lumen.Modules.Identity.Domain.Notifications;
using Lumen.Modules.Identity.Domain.Tokens;
using Lumen.Modules.Identity.Domain.Users;
using MediatR;
using NSubstitute;

namespace Lumen.Modules.Identity.Tests.Application;

public sealed class ResendConfirmationEmailCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IEmailConfirmationTokenRepository _tokenRepository = Substitute.For<IEmailConfirmationTokenRepository>();
    private readonly IEmailConfirmationService _emailConfirmationService = Substitute.For<IEmailConfirmationService>();

    private ResendConfirmationEmailCommandHandler CreateHandler()
        => new(_userRepository, _tokenRepository, _emailConfirmationService);

    [Fact]
    public async Task Handle_InactiveUser_InvalidatesPreviousTokensAndSendsEmail()
    {
        var user = User.Create("alice@test.com", "alice", "hash");

        _userRepository
            .FindByEmailAsync(User.NormalizeEmail("alice@test.com"), Arg.Any<CancellationToken>())
            .Returns(user);

        var handler = CreateHandler();
        var result = await handler.Handle(
            new ResendConfirmationEmailCommand("alice@test.com"),
            CancellationToken.None);

        result.Should().Be(Unit.Value);
        await _tokenRepository.Received(1).InvalidateByUserIdAsync(user.Id, Arg.Any<CancellationToken>());
        await _emailConfirmationService.Received(1).SendConfirmationEmailAsync(
            user, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AlreadyActiveUser_DoesNotSendEmail()
    {
        var user = User.Create("alice@test.com", "alice", "hash");
        user.ConfirmEmail();

        _userRepository
            .FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(user);

        var handler = CreateHandler();
        var result = await handler.Handle(
            new ResendConfirmationEmailCommand("alice@test.com"),
            CancellationToken.None);

        result.Should().Be(Unit.Value);
        await _emailConfirmationService.DidNotReceive()
            .SendConfirmationEmailAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NonExistingEmail_ReturnsWithoutSendingEmail()
    {
        _userRepository
            .FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(
            new ResendConfirmationEmailCommand("unknown@test.com"),
            CancellationToken.None);

        result.Should().Be(Unit.Value);
        await _emailConfirmationService.DidNotReceive()
            .SendConfirmationEmailAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Validator_EmptyEmail_ProducesError()
    {
        var validator = new ResendConfirmationEmailCommandHandler.Validator();
        var result = validator.TestValidate(new ResendConfirmationEmailCommand(""));
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validator_InvalidEmail_ProducesError()
    {
        var validator = new ResendConfirmationEmailCommandHandler.Validator();
        var result = validator.TestValidate(new ResendConfirmationEmailCommand("not-an-email"));
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validator_ValidEmail_HasNoErrors()
    {
        var validator = new ResendConfirmationEmailCommandHandler.Validator();
        var result = validator.TestValidate(new ResendConfirmationEmailCommand("alice@test.com"));
        result.ShouldNotHaveAnyValidationErrors();
    }
}
