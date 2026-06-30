using FluentAssertions;
using FluentValidation.TestHelper;
using Lumen.Modules.Identity.Application.Auth.Refresh;
using Lumen.Modules.Identity.Domain.Configuration;
using Lumen.Modules.Identity.Domain.Security;
using Lumen.Modules.Identity.Domain.Tokens;
using Lumen.Modules.Identity.Domain.Users;
using Lumen.SharedKernel.Exceptions;
using Lumen.SharedKernel.Util;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Lumen.Modules.Identity.Tests.Application;

public sealed class RefreshTokenCommandHandlerTests
{
    private readonly IRefreshTokenRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IJwtService _jwtService = Substitute.For<IJwtService>();
    private readonly IAppSettings _appSettings = Substitute.For<IAppSettings>();

    private RefreshTokenCommandHandler CreateHandler()
        => new(
            _refreshTokenRepository,
            _userRepository,
            _jwtService,
            _appSettings,
            NullLogger<RefreshTokenCommandHandler>.Instance);

    [Fact]
    public async Task Handle_ValidToken_RotatesAndReturnsNewTokens()
    {
        var userId = Guid.NewGuid();
        var rawToken = "valid_refresh_token";
        var tokenHash = Sha256Hasher.ComputeHex(rawToken);
        var token = CreateActiveToken(userId, tokenHash);
        var user = CreateActiveUser();

        _refreshTokenRepository.FindByTokenHashAsync(tokenHash, Arg.Any<CancellationToken>()).Returns(token);
        _userRepository.FindByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);
        _jwtService.GenerateRefreshTokenValue().Returns("new_refresh_value");
        _jwtService.GenerateAccessToken(user).Returns("new_access_token");
        _jwtService.AccessTokenExpiresIn.Returns(900);
        _appSettings.RefreshTokenExpirationDays.Returns(7);

        var handler = CreateHandler();
        var result = await handler.Handle(
            new RefreshTokenCommand(rawToken, "127.0.0.1"),
            CancellationToken.None);

        result.AccessToken.Should().Be("new_access_token");
        result.RefreshToken.Should().Be("new_refresh_value");
        result.ExpiresIn.Should().Be(900);

        await _refreshTokenRepository.Received(1).UpdateAsync(
            Arg.Is<RefreshToken>(t => t.IsRevoked()),
            Arg.Any<CancellationToken>());
        await _refreshTokenRepository.Received(1).InsertAsync(
            Arg.Any<RefreshToken>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TokenNotFound_ThrowsUnauthorizedException()
    {
        _refreshTokenRepository
            .FindByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((RefreshToken?)null);

        var handler = CreateHandler();
        var act = async () => await handler.Handle(
            new RefreshTokenCommand("nonexistent", "ip"),
            CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Handle_ExpiredToken_ThrowsUnauthorizedException()
    {
        var userId = Guid.NewGuid();
        var rawToken = "expired_token";
        var tokenHash = Sha256Hasher.ComputeHex(rawToken);
        var expiredToken = RefreshToken.Create(
            userId: userId,
            tokenHash: tokenHash,
            expiresAt: DateTime.UtcNow.AddSeconds(1),
            createdByIp: "127.0.0.1");

        await Task.Delay(1100);

        _refreshTokenRepository.FindByTokenHashAsync(tokenHash, Arg.Any<CancellationToken>()).Returns(expiredToken);

        var handler = CreateHandler();
        var act = async () => await handler.Handle(
            new RefreshTokenCommand(rawToken, "ip"),
            CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Handle_RevokedToken_RevokesChainAndThrowsUnauthorizedException()
    {
        var userId = Guid.NewGuid();
        var rawToken = "revoked_token";
        var tokenHash = Sha256Hasher.ComputeHex(rawToken);
        var revokedToken = CreateActiveToken(userId, tokenHash);
        revokedToken.Revoke();

        _refreshTokenRepository.FindByTokenHashAsync(tokenHash, Arg.Any<CancellationToken>()).Returns(revokedToken);
        _refreshTokenRepository
            .FindByTokenHashAsync(Arg.Is<string>(h => h != tokenHash), Arg.Any<CancellationToken>())
            .Returns((RefreshToken?)null);

        var handler = CreateHandler();
        var act = async () => await handler.Handle(
            new RefreshTokenCommand(rawToken, "ip"),
            CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Handle_InactiveUser_ThrowsUnauthorizedException()
    {
        var userId = Guid.NewGuid();
        var rawToken = "valid_refresh_token";
        var tokenHash = Sha256Hasher.ComputeHex(rawToken);
        var token = CreateActiveToken(userId, tokenHash);
        var inactiveUser = User.Create("user@test.com", "user", "hash");

        _refreshTokenRepository.FindByTokenHashAsync(tokenHash, Arg.Any<CancellationToken>()).Returns(token);
        _userRepository.FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(inactiveUser);

        var handler = CreateHandler();
        var act = async () => await handler.Handle(
            new RefreshTokenCommand(rawToken, "ip"),
            CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public void Validator_EmptyRefreshToken_ProducesError()
    {
        var validator = new RefreshTokenCommandHandler.Validator();
        var result = validator.TestValidate(new RefreshTokenCommand("", "ip"));
        result.ShouldHaveValidationErrorFor(x => x.RefreshToken);
    }

    [Fact]
    public void Validator_ValidCommand_HasNoErrors()
    {
        var validator = new RefreshTokenCommandHandler.Validator();
        var result = validator.TestValidate(new RefreshTokenCommand("token_value", "127.0.0.1"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    private static RefreshToken CreateActiveToken(Guid userId, string tokenHash)
        => RefreshToken.Create(
            userId: userId,
            tokenHash: tokenHash,
            expiresAt: DateTime.UtcNow.AddDays(7),
            createdByIp: "127.0.0.1");

    private static User CreateActiveUser()
    {
        var user = User.Create("user@test.com", "user", "hash");
        user.ConfirmEmail();
        return user;
    }
}
