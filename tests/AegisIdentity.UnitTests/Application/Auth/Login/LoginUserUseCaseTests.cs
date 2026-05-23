using AegisIdentity.Application.Auth.Login;
using AegisIdentity.Application.Configuration;
using AegisIdentity.Application.Security;
using AegisIdentity.Domain.Tokens;
using AegisIdentity.Domain.Users;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

namespace AegisIdentity.UnitTests.Application.Auth.Login;

public sealed class LoginUserUseCaseTests
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

    public LoginUserUseCaseTests()
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
    public async Task ExecuteAsync_WhenEmailNotFound_ReturnsInvalidCredentials()
    {
        _userRepository.FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ReturnsNull();

        var result = await CreateUseCase().ExecuteAsync(EmailRequest(), ClientIp);

        result.Should().BeOfType<LoginResult.InvalidCredentials>();
    }

    [Fact]
    public async Task ExecuteAsync_WhenUsernameNotFound_ReturnsInvalidCredentials()
    {
        _userRepository.FindByUsernameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ReturnsNull();

        var result = await CreateUseCase().ExecuteAsync(UsernameRequest(), ClientIp);

        result.Should().BeOfType<LoginResult.InvalidCredentials>();
    }

    // ── Email vs username discrimination ──────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_WhenIdentifierContainsAt_QueriesByEmail()
    {
        _userRepository.FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ReturnsNull();

        await CreateUseCase().ExecuteAsync(EmailRequest(), ClientIp);

        await _userRepository.Received(1).FindByEmailAsync(
            User.NormalizeEmail(ValidEmail), Arg.Any<CancellationToken>());
        await _userRepository.DidNotReceive().FindByUsernameAsync(
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenIdentifierHasNoAt_QueriesByUsername()
    {
        _userRepository.FindByUsernameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ReturnsNull();

        await CreateUseCase().ExecuteAsync(UsernameRequest(), ClientIp);

        await _userRepository.Received(1).FindByUsernameAsync(
            ValidUsername, Arg.Any<CancellationToken>());
        await _userRepository.DidNotReceive().FindByEmailAsync(
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ── Wrong password ────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_WhenPasswordIsWrong_ReturnsInvalidCredentials()
    {
        var user = ActiveUser();
        _userRepository.FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(user);
        _passwordHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        var result = await CreateUseCase().ExecuteAsync(EmailRequest(), ClientIp);

        result.Should().BeOfType<LoginResult.InvalidCredentials>();
    }

    [Fact]
    public async Task ExecuteAsync_WhenPasswordIsWrong_IncrementsFailedAttempts()
    {
        var user = ActiveUser();
        _userRepository.FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(user);
        _passwordHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        await CreateUseCase().ExecuteAsync(EmailRequest(), ClientIp);

        await _userRepository.Received(1).UpdateAsync(
            Arg.Is<User>(u => u.FailedLoginAttempts == 1), Arg.Any<CancellationToken>());
    }

    // ── Account locked ────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_WhenAccountIsLocked_ReturnsAccountLockedWithExpiry()
    {
        var lockedUntil = DateTime.UtcNow.AddMinutes(10);
        var user = LockedUser(lockedUntil);
        _userRepository.FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(user);

        var result = await CreateUseCase().ExecuteAsync(EmailRequest(), ClientIp);

        var locked = result.Should().BeOfType<LoginResult.AccountLocked>().Subject;
        locked.LockedUntil.Should().BeCloseTo(lockedUntil, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task ExecuteAsync_WhenAccountIsLocked_DoesNotVerifyPassword()
    {
        var user = LockedUser(DateTime.UtcNow.AddMinutes(10));
        _userRepository.FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(user);

        await CreateUseCase().ExecuteAsync(EmailRequest(), ClientIp);

        _passwordHasher.DidNotReceive().Verify(Arg.Any<string>(), Arg.Any<string>());
    }

    // ── Email not confirmed ───────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_WhenEmailNotConfirmed_ReturnsEmailNotConfirmed()
    {
        var user = InactiveUser();
        SetupEmailLookup(user);
        _passwordHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        var result = await CreateUseCase().ExecuteAsync(EmailRequest(), ClientIp);

        result.Should().BeOfType<LoginResult.EmailNotConfirmed>();
    }

    // ── Happy path ────────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_WithValidCredentials_ReturnsSuccessWithTokens()
    {
        var user = ActiveUser();
        SetupEmailLookup(user);
        _passwordHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        var result = await CreateUseCase().ExecuteAsync(EmailRequest(), ClientIp);

        var success = result.Should().BeOfType<LoginResult.Success>().Subject;
        success.Response.AccessToken.Should().Be(FakeAccessToken);
        success.Response.RefreshToken.Should().Be(FakeRefreshTokenValue);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidCredentials_InsertsRefreshToken()
    {
        var user = ActiveUser();
        SetupEmailLookup(user);
        _passwordHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        await CreateUseCase().ExecuteAsync(EmailRequest(), ClientIp);

        await _refreshTokenRepository.Received(1).InsertAsync(
            Arg.Is<RefreshToken>(t => t.UserId == user.Id && t.CreatedByIp == ClientIp),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithValidCredentials_UpdatesLastLoginAt()
    {
        var user = ActiveUser();
        SetupEmailLookup(user);
        _passwordHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        await CreateUseCase().ExecuteAsync(EmailRequest(), ClientIp);

        await _userRepository.Received(1).UpdateAsync(
            Arg.Is<User>(u => u.LastLoginAt.HasValue), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserHadPreviousFailures_ResetsFailedAttemptsOnSuccess()
    {
        var user = ActiveUser();
        // Simulate one failed attempt (threshold of 10 means it won't lock yet).
        user.RecordFailedLogin(10, TimeSpan.FromMinutes(15));
        SetupEmailLookup(user);
        _passwordHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        await CreateUseCase().ExecuteAsync(EmailRequest(), ClientIp);

        await _userRepository.Received(1).UpdateAsync(
            Arg.Is<User>(u => u.FailedLoginAttempts == 0), Arg.Any<CancellationToken>());
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private LoginUserUseCase CreateUseCase() =>
        new(
            _userRepository,
            _refreshTokenRepository,
            _passwordHasher,
            _jwtService,
            _appSettings,
            NullLogger<LoginUserUseCase>.Instance);

    private static LoginRequest EmailRequest() =>
        new(ValidEmail, ValidPassword);

    private static LoginRequest UsernameRequest() =>
        new(ValidUsername, ValidPassword);

    private static User ActiveUser()
    {
        var user = User.Create(ValidEmail, ValidUsername, FakeHash);
        user.IsActive = true;
        return user;
    }

    private static User InactiveUser() =>
        User.Create(ValidEmail, ValidUsername, FakeHash);

    private static User LockedUser(DateTime lockedUntil)
    {
        var user = ActiveUser();
        // Threshold of 1 guarantees the account locks on the first failed attempt.
        user.RecordFailedLogin(1, lockedUntil - DateTime.UtcNow);
        return user;
    }

    private void SetupEmailLookup(User user)
    {
        _userRepository.FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(user);
    }
}
