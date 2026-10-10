using FluentAssertions;
using Lumen.Modules.Identity.Domain.Notifications;
using Lumen.Modules.Identity.Domain.Users;
using Lumen.Modules.Identity.Infrastructure.Notifications;
using Lumen.SharedKernel.Constants;
using NSubstitute;

namespace Lumen.Modules.Identity.Tests.Application;

public sealed class PasswordChangedNotificationServiceTests
{
    private readonly IEmailService _emailService = Substitute.For<IEmailService>();
    private readonly IEmailTemplateRenderer _templateRenderer = Substitute.For<IEmailTemplateRenderer>();

    private PasswordChangedNotificationService CreateService()
        => new(_emailService, _templateRenderer);

    [Fact]
    public async Task SendPasswordChangedEmailAsync_RendersPasswordChangedTemplate()
    {
        var user = User.Create("bob@test.com", "bob", "hash");
        _templateRenderer
            .Render(Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, string>>())
            .Returns(("<html/>", "text"));

        var service = CreateService();
        await service.SendPasswordChangedEmailAsync(user, CancellationToken.None);

        _templateRenderer.Received(1).Render(
            EmailTemplateNames.PasswordChanged,
            Arg.Any<IReadOnlyDictionary<string, string>>());
    }

    [Fact]
    public async Task SendPasswordChangedEmailAsync_PassesUsernamePlaceholder()
    {
        var user = User.Create("bob@test.com", "bob", "hash");
        IReadOnlyDictionary<string, string>? capturedPlaceholders = null;
        _templateRenderer
            .Render(
                Arg.Any<string>(),
                Arg.Do<IReadOnlyDictionary<string, string>>(p => capturedPlaceholders = p))
            .Returns(("<html/>", "text"));

        var service = CreateService();
        await service.SendPasswordChangedEmailAsync(user, CancellationToken.None);

        capturedPlaceholders.Should().NotBeNull();
        capturedPlaceholders!.Should().ContainKey(EmailPlaceholderKeys.UserName)
            .WhoseValue.Should().Be("bob");
    }

    [Fact]
    public async Task SendPasswordChangedEmailAsync_SendsToUserEmail()
    {
        var user = User.Create("bob@test.com", "bob", "hash");
        _templateRenderer
            .Render(Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, string>>())
            .Returns(("<html/>", "text"));

        var service = CreateService();
        await service.SendPasswordChangedEmailAsync(user, CancellationToken.None);

        await _emailService.Received(1).SendAsync(
            Arg.Is<EmailMessage>(m => m.To == "bob@test.com"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendPasswordChangedEmailAsync_UsesPasswordChangedSubject()
    {
        var user = User.Create("bob@test.com", "bob", "hash");
        _templateRenderer
            .Render(Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, string>>())
            .Returns(("<html/>", "text"));

        var service = CreateService();
        await service.SendPasswordChangedEmailAsync(user, CancellationToken.None);

        await _emailService.Received(1).SendAsync(
            Arg.Is<EmailMessage>(m => m.Subject == EmailSubjects.PasswordChanged),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendPasswordChangedEmailAsync_ForwardsRenderedBodiesVerbatim()
    {
        var user = User.Create("bob@test.com", "bob", "hash");
        _templateRenderer
            .Render(Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, string>>())
            .Returns(("<b>html</b>", "plain text"));

        var service = CreateService();
        await service.SendPasswordChangedEmailAsync(user, CancellationToken.None);

        await _emailService.Received(1).SendAsync(
            Arg.Is<EmailMessage>(m => m.HtmlBody == "<b>html</b>" && m.TextBody == "plain text"),
            Arg.Any<CancellationToken>());
    }
}
