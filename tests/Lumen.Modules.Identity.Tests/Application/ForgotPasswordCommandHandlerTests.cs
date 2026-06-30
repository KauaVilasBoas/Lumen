using FluentAssertions;
using FluentValidation.TestHelper;
using Lumen.Modules.Identity.Application.Auth.ForgotPassword;
using Lumen.Modules.Identity.Domain.Configuration;
using Lumen.Modules.Identity.Domain.Notifications;
using Lumen.Modules.Identity.Domain.Tokens;
using Lumen.Modules.Identity.Domain.Users;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Lumen.Modules.Identity.Tests.Application;

public sealed class ForgotPasswordCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IPasswordResetTokenRepository _tokenRepository = Substitute.For<IPasswordResetTokenRepository>();
    private readonly IEmailService _emailService = Substitute.For<IEmailService>();
    private readonly IEmailTemplateRenderer _templateRenderer = Substitute.For<IEmailTemplateRenderer>();
    private readonly IAppSettings _appSettings = Substitute.For<IAppSettings>();

    private ForgotPasswordCommandHandler CreateHandler()
        => new(
            _userRepository,
            _tokenRepository,
            _emailService,
            _templateRenderer,
            _appSettings,
            NullLogger<ForgotPasswordCommandHandler>.Instance);

    [Fact]
    public async Task Handle_ExistingEmail_InsertsTokenAndSendsEmail()
    {
        var user = User.Create("alice@test.com", "alice", "hash");
        user.ConfirmEmail();

        _userRepository
            .FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(user);
        _appSettings.BaseUrl.Returns("https://app.test");
        _templateRenderer
            .Render(Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, string>>())
            .Returns(("<html/>", "text"));

        var handler = CreateHandler();
        var result = await handler.Handle(new ForgotPasswordCommand("alice@test.com"), CancellationToken.None);

        result.Should().Be(Unit.Value);
        await _tokenRepository.Received(1).InsertAsync(
            Arg.Any<PasswordResetToken>(), Arg.Any<CancellationToken>());
        await _emailService.Received(1).SendAsync(
            Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NonExistingEmail_ReturnsWithoutSendingEmail()
    {
        _userRepository
            .FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(
            new ForgotPasswordCommand("unknown@test.com"), CancellationToken.None);

        result.Should().Be(Unit.Value);
        await _tokenRepository.DidNotReceive().InsertAsync(
            Arg.Any<PasswordResetToken>(), Arg.Any<CancellationToken>());
        await _emailService.DidNotReceive().SendAsync(
            Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TokenStoredAsHash_HasHexLengthOfSha256()
    {
        var user = User.Create("alice@test.com", "alice", "hash");
        user.ConfirmEmail();

        _userRepository
            .FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(user);
        _appSettings.BaseUrl.Returns("https://app.test");
        _templateRenderer
            .Render(Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, string>>())
            .Returns(("<html/>", "text"));

        PasswordResetToken? capturedToken = null;
        _tokenRepository
            .InsertAsync(
                Arg.Do<PasswordResetToken>(t => capturedToken = t),
                Arg.Any<CancellationToken>())
            .Returns(System.Threading.Tasks.Task.CompletedTask);

        var handler = CreateHandler();
        await handler.Handle(new ForgotPasswordCommand("alice@test.com"), CancellationToken.None);

        capturedToken.Should().NotBeNull("o token deve ter sido persistido");
        capturedToken!.TokenHash.Should().HaveLength(64, "SHA-256 hex tem 64 caracteres");
    }

    [Fact]
    public void Validator_EmptyEmail_ProducesError()
    {
        var validator = new ForgotPasswordCommandHandler.Validator();
        var result = validator.TestValidate(new ForgotPasswordCommand(""));
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validator_InvalidEmail_ProducesError()
    {
        var validator = new ForgotPasswordCommandHandler.Validator();
        var result = validator.TestValidate(new ForgotPasswordCommand("not-an-email"));
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validator_ValidEmail_HasNoErrors()
    {
        var validator = new ForgotPasswordCommandHandler.Validator();
        var result = validator.TestValidate(new ForgotPasswordCommand("alice@test.com"));
        result.ShouldNotHaveAnyValidationErrors();
    }
}
