using Lumen.CommandHandlers.Auth.ResendConfirmationEmail;
using Lumen.Domain.Configuration;
using Lumen.Domain.Notifications;
using Lumen.Domain.Tokens;
using Lumen.Domain.Users;
using FluentAssertions;
using NSubstitute;

namespace Lumen.UnitTests.Application.Auth.ResendConfirmationEmail;

public sealed class ResendConfirmationEmailCommandHandlerTests
{
    private const string ExistingEmail = "alice@example.com";
    private const string ExistingUsername = "alice";
    private const string FakeBaseUrl = "https://api.example.com";

    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IEmailConfirmationTokenRepository _tokenRepository = Substitute.For<IEmailConfirmationTokenRepository>();
    private readonly IEmailService _emailService = Substitute.For<IEmailService>();
    private readonly IEmailTemplateRenderer _templateRenderer = Substitute.For<IEmailTemplateRenderer>();
    private readonly IAppSettings _appSettings = Substitute.For<IAppSettings>();

    public ResendConfirmationEmailCommandHandlerTests()
    {
        _appSettings.BaseUrl.Returns(FakeBaseUrl);
        _templateRenderer.Render(Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, string>>())
            .Returns(("<html>confirmation</html>", "confirmation"));
    }

    // ── Email not found — silent no-op (anti-enumeration) ─────────────────

    [Fact]
    public async Task Handle_WhenEmailDoesNotExist_CompletesWithoutError()
    {
        _userRepository.FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var act = () => CreateHandler().Handle(new ResendConfirmationEmailCommandHandler.Command("nobody@example.com"), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Handle_WhenEmailDoesNotExist_DoesNotSendEmail()
    {
        _userRepository.FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        await CreateHandler().Handle(new ResendConfirmationEmailCommandHandler.Command("nobody@example.com"), CancellationToken.None);

        await _emailService.DidNotReceive().SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    // ── Already active user — silent no-op ────────────────────────────────

    [Fact]
    public async Task Handle_WhenUserAlreadyActive_DoesNotSendEmail()
    {
        var activeUser = BuildUser();
        activeUser.IsActive = true;
        activeUser.EmailConfirmedAt = DateTime.UtcNow;

        _userRepository.FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(activeUser);

        await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        await _emailService.DidNotReceive().SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenUserAlreadyActive_DoesNotInvalidatePreviousTokens()
    {
        var activeUser = BuildUser();
        activeUser.IsActive = true;

        _userRepository.FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(activeUser);

        await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        await _tokenRepository.DidNotReceive().InvalidateByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    // ── Happy path ────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenUserIsPending_InvalidatesPreviousTokens()
    {
        var user = BuildUser();
        _userRepository.FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(user);

        await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        await _tokenRepository.Received(1).InvalidateByUserIdAsync(user.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenUserIsPending_InsertsNewToken()
    {
        var user = BuildUser();
        _userRepository.FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(user);

        await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        await _tokenRepository.Received(1).InsertAsync(
            Arg.Any<EmailConfirmationToken>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenUserIsPending_SendsEmailToUser()
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
    public async Task Handle_WhenUserIsPending_RendersEmailConfirmationTemplate()
    {
        var user = BuildUser();
        _userRepository.FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(user);

        await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        _templateRenderer.Received(1).Render(
            "EmailConfirmation",
            Arg.Any<IReadOnlyDictionary<string, string>>());
    }

    [Fact]
    public async Task Handle_WhenUserIsPending_ConfirmationUrlContainsBaseUrl()
    {
        var user = BuildUser();
        _userRepository.FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(user);

        await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        _templateRenderer.Received(1).Render(
            Arg.Any<string>(),
            Arg.Is<IReadOnlyDictionary<string, string>>(d =>
                d.ContainsKey("ConfirmationUrl") &&
                d["ConfirmationUrl"].StartsWith(FakeBaseUrl)));
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private ResendConfirmationEmailCommandHandler CreateHandler() =>
        new(
            _userRepository,
            _tokenRepository,
            _emailService,
            _templateRenderer,
            _appSettings);

    private static ResendConfirmationEmailCommandHandler.Command ValidCommand() =>
        new(ExistingEmail);

    private static User BuildUser() =>
        User.Create(ExistingEmail, ExistingUsername, "$2a$12$fakehash");
}
