using AegisIdentity.CommandHandlers.Auth.Login;
using AegisIdentity.Domain.Configuration;
using AegisIdentity.Domain.Security;
using AegisIdentity.Domain.Tokens;
using AegisIdentity.Domain.Users;
using AegisIdentity.SharedKernel.Constants;
using AegisIdentity.SharedKernel.Exceptions;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

namespace AegisIdentity.UnitTests.Application.Auth.Login;

public sealed class LoginUserCommandHandlerTests
{
    private const string ValidEmail = "alice@example.com";
    private const string ValidUsername = "alice";
    private const string ValidPassword = "Str0ng!Passw0rd-2026";
    private const string FakeHash = "$2a$12$fakehash";
    private const string FakeAccessToken = "header.payload.signature";
    private const string FakeRefreshTokenValue = "opaque-refresh-token";
    private const string ClientIp = "127.0.0.1";

    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IRefreshTokenRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IJwtService _jwtService = Substitute.For<IJwtService>();
    private readonly IAppSettings _appSettings = Substitute.For<IAppSettings>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();

    public LoginUserCommandHandlerTests()
    {
        _appSettings.LockoutThreshold.Returns(5);
        _appSettings.LockoutDuration.Returns(TimeSpan.FromMinutes(15));
        _appSettings.RefreshTokenExpirationDays.Returns(7);

        _jwtService.GenerateAccessToken(Arg.Any<User>()).Returns(FakeAccessToken);
        _jwtService.GenerateRefreshTokenValue().Returns(FakeRefreshTokenValue);
        _jwtService.AccessTokenExpiresIn.Returns(900);
    }

    // ── User not found ────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenEmailNotFound_ThrowsUnauthorizedException()
    {
        _userRepository.FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ReturnsNull();

        var act = () => CreateHandler().Handle(EmailCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Handle_WhenUsernameNotFound_ThrowsUnauthorizedException()
    {
        _userRepository.FindByUsernameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ReturnsNull();

        var act = () => CreateHandler().Handle(UsernameCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_StillCallsPasswordVerifyToPreventTimingAttack()
    {
        // Arrange — user does not exist; Verify must still be called once so that
        // response time is indistinguishable from a wrong-password attempt.
        _userRepository.FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ReturnsNull();

        // Act
        await Assert.ThrowsAsync<UnauthorizedException>(
            () => CreateHandler().Handle(EmailCommand(), CancellationToken.None));

        // Assert — dummy-hash Verify was invoked exactly once
        _passwordHasher.Received(1).Verify(ValidPassword, Arg.Any<string>());
    }

    // ── Email vs username discrimination ──────────────────────────────────

    [Fact]
    public async Task Handle_WhenIdentifierContainsAt_QueriesByEmail()
    {
        _userRepository.FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ReturnsNull();

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => CreateHandler().Handle(EmailCommand(), CancellationToken.None));

        await _userRepository.Received(1).FindByEmailAsync(
            User.NormalizeEmail(ValidEmail), Arg.Any<CancellationToken>());
        await _userRepository.DidNotReceive().FindByUsernameAsync(
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenIdentifierHasNoAt_QueriesByUsername()
    {
        _userRepository.FindByUsernameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ReturnsNull();

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => CreateHandler().Handle(UsernameCommand(), CancellationToken.None));

        await _userRepository.Received(1).FindByUsernameAsync(
            ValidUsername, Arg.Any<CancellationToken>());
        await _userRepository.DidNotReceive().FindByEmailAsync(
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ── Wrong password ────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenPasswordIsWrong_ThrowsUnauthorizedException()
    {
        var user = ActiveUser();
        _userRepository.FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        var act = () => CreateHandler().Handle(EmailCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Handle_WhenPasswordIsWrong_IncrementsFailedAttempts()
    {
        var user = ActiveUser();
        _userRepository.FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => CreateHandler().Handle(EmailCommand(), CancellationToken.None));

        await _userRepository.Received(1).UpdateAsync(
            Arg.Is<User>(u => u.FailedLoginAttempts == 1), Arg.Any<CancellationToken>());
    }

    // ── Account locked ────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenAccountIsLocked_ThrowsAccountLockedExceptionWithExpiry()
    {
        var lockedUntil = DateTime.UtcNow.AddMinutes(10);
        var user = LockedUser(lockedUntil);
        _userRepository.FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);

        var act = () => CreateHandler().Handle(EmailCommand(), CancellationToken.None);

        var exception = await act.Should().ThrowAsync<AccountLockedException>();
        exception.Which.LockedUntil.Should().BeCloseTo(lockedUntil, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Handle_WhenAccountIsLocked_DoesNotVerifyPassword()
    {
        var user = LockedUser(DateTime.UtcNow.AddMinutes(10));
        _userRepository.FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);

        await Assert.ThrowsAsync<AccountLockedException>(
            () => CreateHandler().Handle(EmailCommand(), CancellationToken.None));

        _passwordHasher.DidNotReceive().Verify(Arg.Any<string>(), Arg.Any<string>());
    }

    // ── Email not confirmed ───────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenEmailNotConfirmed_ThrowsForbiddenException()
    {
        var user = InactiveUser();
        _userRepository.FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        var act = () => CreateHandler().Handle(EmailCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    // ── Happy path ────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithValidCredentials_ReturnsSuccessWithTokens()
    {
        var user = ActiveUser();
        _userRepository.FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        var result = await CreateHandler().Handle(EmailCommand(), CancellationToken.None);

        result.AccessToken.Should().Be(FakeAccessToken);
        result.RefreshToken.Should().Be(FakeRefreshTokenValue);
    }

    [Fact]
    public async Task Handle_WithValidCredentials_ReturnsTokenTypeBearer()
    {
        var user = ActiveUser();
        _userRepository.FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        var result = await CreateHandler().Handle(EmailCommand(), CancellationToken.None);

        result.TokenType.Should().Be(TokenTypes.Bearer);
    }

    [Fact]
    public async Task Handle_WithValidCredentials_InsertsRefreshToken()
    {
        var user = ActiveUser();
        _userRepository.FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        await CreateHandler().Handle(EmailCommand(), CancellationToken.None);

        await _refreshTokenRepository.Received(1).InsertAsync(
            Arg.Is<RefreshToken>(t => t.UserId == user.Id && t.CreatedByIp == ClientIp),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithValidCredentials_UpdatesLastLoginAt()
    {
        var user = ActiveUser();
        _userRepository.FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        await CreateHandler().Handle(EmailCommand(), CancellationToken.None);

        await _userRepository.Received(1).UpdateAsync(
            Arg.Is<User>(u => u.LastLoginAt.HasValue), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenUserHadPreviousFailures_ResetsFailedAttemptsOnSuccess()
    {
        var user = ActiveUser();
        user.RecordFailedLogin(10, TimeSpan.FromMinutes(15));
        _userRepository.FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        await CreateHandler().Handle(EmailCommand(), CancellationToken.None);

        await _userRepository.Received(1).UpdateAsync(
            Arg.Is<User>(u => u.FailedLoginAttempts == 0), Arg.Any<CancellationToken>());
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private LoginUserCommandHandler CreateHandler() =>
        new(
            _userRepository,
            _refreshTokenRepository,
            _passwordHasher,
            _jwtService,
            _appSettings,
            _publisher,
            NullLogger<LoginUserCommandHandler>.Instance);

    private static LoginUserCommandHandler.Command EmailCommand() =>
        new(ValidEmail, ValidPassword, ClientIp);

    private static LoginUserCommandHandler.Command UsernameCommand() =>
        new(ValidUsername, ValidPassword, ClientIp);

    private static User ActiveUser()
    {
        var user = User.Create(ValidEmail, ValidUsername, FakeHash);
        user.ConfirmEmail();
        return user;
    }

    private static User InactiveUser() =>
        User.Create(ValidEmail, ValidUsername, FakeHash);

    private static User LockedUser(DateTime lockedUntil)
    {
        var user = ActiveUser();
        user.RecordFailedLogin(1, lockedUntil - DateTime.UtcNow);
        return user;
    }
}
