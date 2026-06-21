using Lumen.CommandHandlers.Auth.ForgotPassword;
using Lumen.Domain.Configuration;
using Lumen.Domain.Notifications;
using Lumen.Domain.Tokens;
using Lumen.Domain.Users;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Lumen.UnitTests.Application.Auth.ForgotPassword;

public sealed class ForgotPasswordCommandHandlerTests
{
    private const string ExistingEmail = "alice@example.com";
    private const string ExistingUsername = "alice";
    private const string FakeBaseUrl = "https://api.example.com";

    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IPasswordResetTokenRepository _tokenRepository = Substitute.For<IPasswordResetTokenRepository>();
    private readonly IEmailService _emailService = Substitute.For<IEmailService>();
    private readonly IEmailTemplateRenderer _templateRenderer = Substitute.For<IEmailTemplateRenderer>();
    private readonly IAppSettings _appSettings = Substitute.For<IAppSettings>();

    public ForgotPasswordCommandHandlerTests()
    {
        _appSettings.BaseUrl.Returns(FakeBaseUrl);
        _templateRenderer.Render(Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, string>>())
            .Returns(("<html>reset</html>", "reset"));
    }

    // ── Email not found — response must be identical to the happy path ─────

    [Fact]
    public async Task Handle_WhenEmailDoesNotExist_CompletesWithoutError()
    {
        _userRepository.FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var act = () => CreateHandler().Handle(new ForgotPasswordCommandHandler.Command("unknown@example.com"), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Handle_WhenEmailDoesNotExist_DoesNotInsertToken()
    {
        _userRepository.FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        await CreateHandler().Handle(new ForgotPasswordCommandHandler.Command("unknown@example.com"), CancellationToken.None);

        await _tokenRepository.DidNotReceive().InsertAsync(Arg.Any<PasswordResetToken>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenEmailDoesNotExist_DoesNotSendEmail()
    {
        _userRepository.FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        await CreateHandler().Handle(new ForgotPasswordCommandHandler.Command("unknown@example.com"), CancellationToken.None);

        await _emailService.DidNotReceive().SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    // ── Happy path ────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenEmailExists_InsertsPasswordResetToken()
    {
        _userRepository.FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(BuildUser());

        await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        await _tokenRepository.Received(1).InsertAsync(
            Arg.Any<PasswordResetToken>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenEmailExists_InsertsTokenWithFutureExpiry()
    {
        _userRepository.FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(BuildUser());

        await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        await _tokenRepository.Received(1).InsertAsync(
            Arg.Is<PasswordResetToken>(t => t.ExpiresAt > DateTime.UtcNow),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenEmailExists_SendsEmailToUser()
    {
        var user = BuildUser();
        _userRepository.FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(user);

        await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        await _emailService.Received(1).SendAsync(
            Arg.Is<EmailMessage>(m => m.To == user.Email),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenEmailExists_RendersPasswordResetTemplate()
    {
        _userRepository.FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(BuildUser());

        await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        _templateRenderer.Received(1).Render(
            "PasswordReset",
            Arg.Any<IReadOnlyDictionary<string, string>>());
    }

    [Fact]
    public async Task Handle_WhenEmailExists_ResetUrlContainsBaseUrl()
    {
        _userRepository.FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(BuildUser());

        await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        _templateRenderer.Received(1).Render(
            Arg.Any<string>(),
            Arg.Is<IReadOnlyDictionary<string, string>>(d =>
                d.ContainsKey("ResetUrl") &&
                d["ResetUrl"].StartsWith(FakeBaseUrl)));
    }

    [Fact]
    public async Task Handle_WhenEmailExists_ResetUrlDoesNotContainRawToken()
    {
        PasswordResetToken? insertedToken = null;
        _userRepository.FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(BuildUser());
        await _tokenRepository.InsertAsync(
            Arg.Do<PasswordResetToken>(t => insertedToken = t),
            Arg.Any<CancellationToken>());

        string? capturedResetUrl = null;
        _templateRenderer.When(r =>
            r.Render(Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, string>>()))
            .Do(call =>
            {
                var placeholders = call.Arg<IReadOnlyDictionary<string, string>>();
                capturedResetUrl = placeholders.GetValueOrDefault("ResetUrl");
            });
        _templateRenderer.Render(Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, string>>())
            .Returns(("<html/>", "txt"));

        await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        insertedToken.Should().NotBeNull();
        capturedResetUrl.Should().NotBeNull();
        capturedResetUrl.Should().NotContain(insertedToken!.TokenHash);
    }

    // ── Email normalisation ────────────────────────────────────────────────

    [Fact]
    public async Task Handle_NormalisesEmailBeforeLookup()
    {
        _userRepository.FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        await CreateHandler().Handle(
            new ForgotPasswordCommandHandler.Command("  ALICE@Example.COM  "),
            CancellationToken.None);

        await _userRepository.Received(1).FindByEmailAsync(
            "alice@example.com",
            Arg.Any<CancellationToken>());
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private ForgotPasswordCommandHandler CreateHandler() =>
        new(
            _userRepository,
            _tokenRepository,
            _emailService,
            _templateRenderer,
            _appSettings,
            NullLogger<ForgotPasswordCommandHandler>.Instance);

    private static ForgotPasswordCommandHandler.Command ValidCommand() =>
        new(ExistingEmail);

    private static User BuildUser() =>
        User.Create(ExistingEmail, ExistingUsername, "$2a$12$fakehash");
}
