using Lumen.Domain.Configuration;
using Lumen.Domain.Notifications;
using Lumen.Domain.Tokens;
using Lumen.Domain.Users;
using Lumen.SharedKernel.Constants;
using FluentAssertions;
using NSubstitute;

namespace Lumen.UnitTests.Domain.Notifications;

public sealed class EmailConfirmationServiceTests
{
    private const string FakeBaseUrl = "https://api.example.com";

    private readonly IEmailConfirmationTokenRepository _tokenRepository =
        Substitute.For<IEmailConfirmationTokenRepository>();

    private readonly IEmailService _emailService = Substitute.For<IEmailService>();
    private readonly IEmailTemplateRenderer _templateRenderer = Substitute.For<IEmailTemplateRenderer>();
    private readonly IAppSettings _appSettings = Substitute.For<IAppSettings>();

    public EmailConfirmationServiceTests()
    {
        _appSettings.BaseUrl.Returns(FakeBaseUrl);
        _templateRenderer
            .Render(Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, string>>())
            .Returns(("<html>confirm</html>", "confirm text"));
    }

    [Fact]
    public async Task SendConfirmationEmail_InsertsTokenInRepository()
    {
        var user = BuildUser();

        await CreateService().SendConfirmationEmailAsync(user);

        await _tokenRepository.Received(1).InsertAsync(
            Arg.Any<EmailConfirmationToken>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendConfirmationEmail_TokenIsLinkedToUser()
    {
        var user = BuildUser();

        await CreateService().SendConfirmationEmailAsync(user);

        await _tokenRepository.Received(1).InsertAsync(
            Arg.Is<EmailConfirmationToken>(t => t.UserId == user.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendConfirmationEmail_TokenExpiresIn24Hours()
    {
        var user = BuildUser();
        var before = DateTime.UtcNow;

        await CreateService().SendConfirmationEmailAsync(user);

        await _tokenRepository.Received(1).InsertAsync(
            Arg.Is<EmailConfirmationToken>(t =>
                t.ExpiresAt >= before.AddHours(24) &&
                t.ExpiresAt <= DateTime.UtcNow.AddHours(24).AddSeconds(5)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendConfirmationEmail_SendsEmailToUserAddress()
    {
        var user = BuildUser();

        await CreateService().SendConfirmationEmailAsync(user);

        await _emailService.Received(1).SendAsync(
            Arg.Is<EmailMessage>(m => m.To == user.Email),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendConfirmationEmail_UsesEmailConfirmationSubject()
    {
        var user = BuildUser();

        await CreateService().SendConfirmationEmailAsync(user);

        await _emailService.Received(1).SendAsync(
            Arg.Is<EmailMessage>(m => m.Subject == EmailSubjects.EmailConfirmation),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendConfirmationEmail_RendersEmailConfirmationTemplate()
    {
        var user = BuildUser();

        await CreateService().SendConfirmationEmailAsync(user);

        _templateRenderer.Received(1).Render(
            EmailTemplateNames.EmailConfirmation,
            Arg.Any<IReadOnlyDictionary<string, string>>());
    }

    [Fact]
    public async Task SendConfirmationEmail_ConfirmationUrlContainsBaseUrl()
    {
        var user = BuildUser();

        await CreateService().SendConfirmationEmailAsync(user);

        _templateRenderer.Received(1).Render(
            Arg.Any<string>(),
            Arg.Is<IReadOnlyDictionary<string, string>>(d =>
                d.ContainsKey(EmailPlaceholderKeys.ConfirmationUrl) &&
                d[EmailPlaceholderKeys.ConfirmationUrl].StartsWith(FakeBaseUrl)));
    }

    [Fact]
    public async Task SendConfirmationEmail_IncludesUsernameInPlaceholders()
    {
        var user = BuildUser();

        await CreateService().SendConfirmationEmailAsync(user);

        _templateRenderer.Received(1).Render(
            Arg.Any<string>(),
            Arg.Is<IReadOnlyDictionary<string, string>>(d =>
                d.ContainsKey(EmailPlaceholderKeys.UserName) &&
                d[EmailPlaceholderKeys.UserName] == user.Username));
    }

    [Fact]
    public async Task SendConfirmationEmail_CompletesWithoutError()
    {
        var user = BuildUser();

        var act = () => CreateService().SendConfirmationEmailAsync(user);

        await act.Should().NotThrowAsync();
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private EmailConfirmationService CreateService() =>
        new(_tokenRepository, _emailService, _templateRenderer, _appSettings);

    private static User BuildUser() =>
        User.Create("alice@example.com", "alice", "$2a$12$fakehash");
}
