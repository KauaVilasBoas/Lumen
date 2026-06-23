using Lumen.Domain.Notifications;
using Lumen.Domain.Tokens;
using Lumen.Domain.Users;
using Lumen.SharedKernel.Constants;
using FluentAssertions;
using NSubstitute;

namespace Lumen.UnitTests.Domain.Users;

public sealed class UserPasswordServiceTests
{
    private readonly IRefreshTokenRepository _refreshTokenRepository =
        Substitute.For<IRefreshTokenRepository>();

    private readonly IEmailService _emailService = Substitute.For<IEmailService>();
    private readonly IEmailTemplateRenderer _templateRenderer = Substitute.For<IEmailTemplateRenderer>();

    public UserPasswordServiceTests()
    {
        _templateRenderer
            .Render(Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, string>>())
            .Returns(("<html/>", "txt"));
    }

    // ── RevokeAllRefreshTokensAsync ───────────────────────────────────────

    [Fact]
    public async Task RevokeAllRefreshTokens_WhenNoTokens_CompletesWithoutError()
    {
        _refreshTokenRepository
            .FindByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<RefreshToken>());

        var act = () => CreateService().RevokeAllRefreshTokensAsync(Guid.NewGuid());

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task RevokeAllRefreshTokens_RevokesOnlyActiveTokens()
    {
        var userId = Guid.NewGuid();
        var activeToken = RefreshToken.Create(userId, "hash1", DateTime.UtcNow.AddDays(7), "127.0.0.1");
        var revokedToken = RefreshToken.Create(userId, "hash2", DateTime.UtcNow.AddDays(7), "127.0.0.1");
        revokedToken.Revoke();

        _refreshTokenRepository
            .FindByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new[] { activeToken, revokedToken });

        await CreateService().RevokeAllRefreshTokensAsync(userId);

        activeToken.IsRevoked().Should().BeTrue();
        await _refreshTokenRepository.Received(1).UpdateAsync(activeToken, Arg.Any<CancellationToken>());
        await _refreshTokenRepository.DidNotReceive().UpdateAsync(revokedToken, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RevokeAllRefreshTokens_MultipleActiveTokens_RevokesAll()
    {
        var userId = Guid.NewGuid();
        var token1 = RefreshToken.Create(userId, "hash1", DateTime.UtcNow.AddDays(7), "127.0.0.1");
        var token2 = RefreshToken.Create(userId, "hash2", DateTime.UtcNow.AddDays(7), "127.0.0.2");

        _refreshTokenRepository
            .FindByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new[] { token1, token2 });

        await CreateService().RevokeAllRefreshTokensAsync(userId);

        token1.IsRevoked().Should().BeTrue();
        token2.IsRevoked().Should().BeTrue();
        await _refreshTokenRepository.Received(2).UpdateAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
    }

    // ── SendPasswordChangedEmailAsync ─────────────────────────────────────

    [Fact]
    public async Task SendPasswordChangedEmail_SendsEmailToUserAddress()
    {
        var user = User.Create("alice@example.com", "alice", "hash");

        await CreateService().SendPasswordChangedEmailAsync(user);

        await _emailService.Received(1).SendAsync(
            Arg.Is<EmailMessage>(m => m.To == user.Email),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendPasswordChangedEmail_RendersPasswordChangedTemplate()
    {
        var user = User.Create("alice@example.com", "alice", "hash");

        await CreateService().SendPasswordChangedEmailAsync(user);

        _templateRenderer.Received(1).Render(
            EmailTemplateNames.PasswordChanged,
            Arg.Any<IReadOnlyDictionary<string, string>>());
    }

    [Fact]
    public async Task SendPasswordChangedEmail_UsesPasswordChangedSubject()
    {
        var user = User.Create("alice@example.com", "alice", "hash");

        await CreateService().SendPasswordChangedEmailAsync(user);

        await _emailService.Received(1).SendAsync(
            Arg.Is<EmailMessage>(m => m.Subject == EmailSubjects.PasswordChanged),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendPasswordChangedEmail_IncludesUsernameInPlaceholders()
    {
        var user = User.Create("alice@example.com", "alice", "hash");

        await CreateService().SendPasswordChangedEmailAsync(user);

        _templateRenderer.Received(1).Render(
            Arg.Any<string>(),
            Arg.Is<IReadOnlyDictionary<string, string>>(d =>
                d.ContainsKey(EmailPlaceholderKeys.UserName) &&
                d[EmailPlaceholderKeys.UserName] == user.Username));
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private UserPasswordService CreateService() =>
        new(_refreshTokenRepository, _emailService, _templateRenderer);
}
