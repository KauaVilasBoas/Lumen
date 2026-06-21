using Lumen.CommandHandlers.Auth.ResetPassword;
using Lumen.Domain.Notifications;
using Lumen.Domain.Security;
using Lumen.Domain.Tokens;
using Lumen.Domain.Users;
using Lumen.SharedKernel.Exceptions;
using Lumen.SharedKernel.Util;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Lumen.UnitTests.Application.Auth.ResetPassword;

public sealed class ResetPasswordCommandHandlerTests
{
    private const string ExistingEmail = "alice@example.com";
    private const string ExistingUsername = "alice";
    private const string ValidNewPassword = "NewStr0ng!Pass";
    private const string FakePasswordHash = "$2a$12$newhash";

    private readonly IPasswordResetTokenRepository _tokenRepository =
        Substitute.For<IPasswordResetTokenRepository>();

    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();

    private readonly IRefreshTokenRepository _refreshTokenRepository =
        Substitute.For<IRefreshTokenRepository>();

    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IPasswordValidator _passwordValidator = Substitute.For<IPasswordValidator>();
    private readonly IEmailService _emailService = Substitute.For<IEmailService>();
    private readonly IEmailTemplateRenderer _templateRenderer = Substitute.For<IEmailTemplateRenderer>();

    public ResetPasswordCommandHandlerTests()
    {
        _passwordValidator
            .ValidatePasswordAsync(Arg.Any<PasswordValidationContext>(), Arg.Any<CancellationToken>())
            .Returns(PasswordValidationResult.Success);

        _passwordHasher.Hash(Arg.Any<string>()).Returns(FakePasswordHash);

        _refreshTokenRepository
            .FindByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<RefreshToken>());

        _templateRenderer
            .Render(Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, string>>())
            .Returns(("<html/>", "txt"));
    }

    // ── Token not found ───────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenTokenNotFound_ThrowsUnauthorizedException()
    {
        _tokenRepository.FindByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((PasswordResetToken?)null);

        var act = () => CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    // ── Token already used ────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenTokenAlreadyUsed_ThrowsUnauthorizedException()
    {
        var token = BuildValidToken();
        token.MarkAsUsed();

        _tokenRepository.FindByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(token);

        var act = () => CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    // ── User not found ────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenUserNotFound_ThrowsUnauthorizedException()
    {
        _tokenRepository.FindByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(BuildValidToken());
        _userRepository.FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var act = () => CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    // ── Password policy failure ───────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenPasswordFailsPolicy_ThrowsValidationException()
    {
        SetupValidTokenAndUser();

        _passwordValidator
            .ValidatePasswordAsync(Arg.Any<PasswordValidationContext>(), Arg.Any<CancellationToken>())
            .Returns(PasswordValidationResult.Failure(["Senha fraca."]));

        var act = () => CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<SharedKernel.Exceptions.ValidationException>();
    }

    // ── Happy path ────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenTokenValid_MarksTokenAsUsed()
    {
        var (token, _) = SetupValidTokenAndUser();

        await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        token.IsUsed().Should().BeTrue();
        await _tokenRepository.Received(1).UpdateAsync(token, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTokenValid_UpdatesPasswordHash()
    {
        var (_, user) = SetupValidTokenAndUser();

        await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        user.PasswordHash.Should().Be(FakePasswordHash);
        await _userRepository.Received(1).UpdateAsync(user, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTokenValid_RevokesActiveRefreshTokens()
    {
        var (_, user) = SetupValidTokenAndUser();
        var activeToken = BuildActiveRefreshToken(user.Id);

        _refreshTokenRepository
            .FindByUserIdAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(new[] { activeToken });

        await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        activeToken.IsRevoked().Should().BeTrue();
        await _refreshTokenRepository.Received(1).UpdateAsync(activeToken, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTokenValid_SendsPasswordChangedEmail()
    {
        var (_, user) = SetupValidTokenAndUser();

        await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        await _emailService.Received(1).SendAsync(
            Arg.Is<EmailMessage>(m => m.To == user.Email),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTokenValid_RendersPasswordChangedTemplate()
    {
        SetupValidTokenAndUser();

        await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        _templateRenderer.Received(1).Render(
            "PasswordChanged",
            Arg.Any<IReadOnlyDictionary<string, string>>());
    }

    [Fact]
    public async Task Handle_WhenTokenValid_CompletesWithoutError()
    {
        SetupValidTokenAndUser();

        var act = () => CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private (PasswordResetToken Token, User User) SetupValidTokenAndUser()
    {
        var token = BuildValidToken();
        var user = BuildUser();

        _tokenRepository.FindByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(token);
        _userRepository.FindByIdAsync(token.UserId, Arg.Any<CancellationToken>())
            .Returns(user);

        return (token, user);
    }

    private ResetPasswordCommandHandler CreateHandler() =>
        new(
            _tokenRepository,
            _userRepository,
            _refreshTokenRepository,
            _passwordHasher,
            _passwordValidator,
            _emailService,
            _templateRenderer,
            NullLogger<ResetPasswordCommandHandler>.Instance);

    private static ResetPasswordCommandHandler.Command ValidCommand() =>
        new("some-raw-token", ValidNewPassword);

    private static PasswordResetToken BuildValidToken() =>
        PasswordResetToken.Create(
            Guid.NewGuid(),
            Sha256Hasher.ComputeHex("some-raw-token"),
            DateTime.UtcNow.AddMinutes(30));

    private static User BuildUser() =>
        User.Create(ExistingEmail, ExistingUsername, "$2a$12$oldhash");

    private static RefreshToken BuildActiveRefreshToken(Guid userId) =>
        RefreshToken.Create(userId, "hash", DateTime.UtcNow.AddDays(7), "127.0.0.1");
}
