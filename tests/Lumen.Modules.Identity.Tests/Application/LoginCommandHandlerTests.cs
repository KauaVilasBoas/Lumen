using FluentAssertions;
using Lumen.Modularity;
using Lumen.Modules.Identity.Application.Auth.Login;
using Lumen.Modules.Identity.Contracts.Events;
using Lumen.Modules.Identity.Domain.Configuration;
using Lumen.Modules.Identity.Domain.Security;
using Lumen.Modules.Identity.Domain.Tokens;
using Lumen.Modules.Identity.Domain.Users;
using Lumen.SharedKernel.Constants;
using Lumen.SharedKernel.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Lumen.Modules.Identity.Tests.Application;

public sealed class LoginCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IRefreshTokenRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IJwtService _jwtService = Substitute.For<IJwtService>();
    private readonly IAppSettings _appSettings = Substitute.For<IAppSettings>();
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();

    private LoginCommandHandler CreateHandler()
        => new(
            _userRepository,
            _refreshTokenRepository,
            _passwordHasher,
            _jwtService,
            _appSettings,
            _eventBus,
            NullLogger<LoginCommandHandler>.Instance);

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsTokensAndPublishesEvent()
    {
        var user = CreateActiveUser();
        _userRepository.FindByUsernameAsync("alice", Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("correct", user.PasswordHash).Returns(true);
        _jwtService.GenerateAccessToken(user).Returns("access_token");
        _jwtService.GenerateRefreshTokenValue().Returns("refresh_value");
        _jwtService.AccessTokenExpiresIn.Returns(900);
        _appSettings.RefreshTokenExpirationDays.Returns(7);
        _appSettings.LockoutThreshold.Returns(5);
        _appSettings.LockoutDuration.Returns(TimeSpan.FromMinutes(15));

        var handler = CreateHandler();
        var result = await handler.Handle(new LoginCommandHandler.Command("alice", "correct", "127.0.0.1"), CancellationToken.None);

        result.AccessToken.Should().Be("access_token");
        result.RefreshToken.Should().Be("refresh_value");
        result.TokenType.Should().Be(TokenTypes.Bearer);

        await _eventBus.Received(1).PublishAsync(
            Arg.Is<UserLoggedInEvent>(e => e.UserId == user.Id && e.Username == "alice"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsUnauthorizedException()
    {
        _userRepository.FindByUsernameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((User?)null);
        _passwordHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        var handler = CreateHandler();
        var act = async () => await handler.Handle(new LoginCommandHandler.Command("unknown", "pass", "ip"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Handle_AccountLocked_ThrowsAccountLockedException()
    {
        var user = CreateLockedUser();
        _userRepository.FindByUsernameAsync("alice", Arg.Any<CancellationToken>()).Returns(user);

        var handler = CreateHandler();
        var act = async () => await handler.Handle(new LoginCommandHandler.Command("alice", "pass", "ip"), CancellationToken.None);

        await act.Should().ThrowAsync<AccountLockedException>();
    }

    [Fact]
    public async Task Handle_WrongPassword_AccountLockedOnThreshold_PublishesLockedOutEvent()
    {
        var user = CreateActiveUserWithFailedAttempts(4);
        _userRepository.FindByUsernameAsync("alice", Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("wrong", user.PasswordHash).Returns(false);
        _appSettings.LockoutThreshold.Returns(5);
        _appSettings.LockoutDuration.Returns(TimeSpan.FromMinutes(15));

        var handler = CreateHandler();

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => handler.Handle(new LoginCommandHandler.Command("alice", "wrong", "ip"), CancellationToken.None));

        await _eventBus.Received(1).PublishAsync(
            Arg.Is<UserLockedOutEvent>(e => e.UserId == user.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InactiveUser_ThrowsForbiddenException()
    {
        var user = CreateInactiveUser();
        _userRepository.FindByUsernameAsync("alice", Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("correct", user.PasswordHash).Returns(true);

        var handler = CreateHandler();
        var act = async () => await handler.Handle(new LoginCommandHandler.Command("alice", "correct", "ip"), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    private static User CreateActiveUser()
    {
        var user = User.Create("alice@test.com", "alice", "hashed");
        user.ConfirmEmail();
        return user;
    }

    private static User CreateLockedUser()
    {
        var user = User.Create("alice@test.com", "alice", "hashed");
        user.ConfirmEmail();
        user.RecordFailedLogin(1, TimeSpan.FromMinutes(15));
        return user;
    }

    private static User CreateActiveUserWithFailedAttempts(int attempts)
    {
        var user = User.Create("alice@test.com", "alice", "hashed");
        user.ConfirmEmail();
        for (var i = 0; i < attempts; i++)
            user.RecordFailedLogin(5, TimeSpan.FromMinutes(15));
        return user;
    }

    private static User CreateInactiveUser()
        => User.Create("alice@test.com", "alice", "hashed");
}
