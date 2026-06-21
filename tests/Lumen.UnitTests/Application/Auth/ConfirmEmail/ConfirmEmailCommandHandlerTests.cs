using Lumen.CommandHandlers.Auth.ConfirmEmail;
using Lumen.Domain.Tokens;
using Lumen.Domain.Users;
using Lumen.SharedKernel.Exceptions;
using Lumen.SharedKernel.Util;
using FluentAssertions;
using NSubstitute;

namespace Lumen.UnitTests.Application.Auth.ConfirmEmail;

public sealed class ConfirmEmailCommandHandlerTests
{
    private const string ExistingEmail = "alice@example.com";
    private const string ExistingUsername = "alice";

    private readonly IEmailConfirmationTokenRepository _tokenRepository =
        Substitute.For<IEmailConfirmationTokenRepository>();

    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();

    // ── Token not found ───────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenTokenNotFound_ThrowsUnauthorizedException()
    {
        _tokenRepository.FindByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((EmailConfirmationToken?)null);

        var act = () => CreateHandler().Handle(new ConfirmEmailCommandHandler.Command("invalid-raw-token"), CancellationToken.None);

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

    // ── Token expired ─────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenTokenExpired_ThrowsUnauthorizedException()
    {
        var expiredToken = EmailConfirmationToken.Create(
            Guid.NewGuid(),
            Sha256Hasher.ComputeHex("some-raw-token"),
            DateTime.UtcNow.AddSeconds(1));

        await Task.Delay(1100);

        _tokenRepository.FindByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(expiredToken);

        var act = () => CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    // ── User not found ────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenUserNotFound_ThrowsUnauthorizedException()
    {
        var token = BuildValidToken();
        _tokenRepository.FindByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(token);
        _userRepository.FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var act = () => CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    // ── Happy path ────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenTokenValid_MarksTokenAsUsed()
    {
        var (token, user) = SetupHappyPath();

        await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        token.IsUsed().Should().BeTrue();
        await _tokenRepository.Received(1).UpdateAsync(token, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTokenValid_ActivatesUser()
    {
        var (_, user) = SetupHappyPath();

        await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        user.IsActive.Should().BeTrue();
        user.EmailConfirmedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_WhenTokenValid_PersistsUser()
    {
        var (_, user) = SetupHappyPath();

        await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        await _userRepository.Received(1).UpdateAsync(user, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTokenValid_CompletesWithoutError()
    {
        SetupHappyPath();

        var act = () => CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private (EmailConfirmationToken Token, User User) SetupHappyPath()
    {
        var token = BuildValidToken();
        var user = BuildUser();

        _tokenRepository.FindByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(token);
        _userRepository.FindByIdAsync(token.UserId, Arg.Any<CancellationToken>())
            .Returns(user);

        return (token, user);
    }

    private ConfirmEmailCommandHandler CreateHandler() =>
        new(_tokenRepository, _userRepository);

    private static ConfirmEmailCommandHandler.Command ValidCommand() =>
        new("some-raw-token");

    private static EmailConfirmationToken BuildValidToken() =>
        EmailConfirmationToken.Create(
            Guid.NewGuid(),
            Sha256Hasher.ComputeHex("some-raw-token"),
            DateTime.UtcNow.AddHours(24));

    private static User BuildUser() =>
        User.Create(ExistingEmail, ExistingUsername, "$2a$12$fakehash");
}
