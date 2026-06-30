using FluentAssertions;
using Lumen.Modules.Identity.Application.Auth.Logout;
using Lumen.Modules.Identity.Domain.Tokens;
using Lumen.SharedKernel.Exceptions;
using Lumen.SharedKernel.Util;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Lumen.Modules.Identity.Tests.Application;

public sealed class LogoutCommandHandlerTests
{
    private readonly IRefreshTokenRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();

    private LogoutCommandHandler CreateHandler()
        => new(_refreshTokenRepository, NullLogger<LogoutCommandHandler>.Instance);

    [Fact]
    public async Task Handle_NullRefreshToken_ReturnsWithoutRevoking()
    {
        var handler = CreateHandler();
        var result = await handler.Handle(
            new LogoutCommand(null, Guid.NewGuid(), "127.0.0.1"),
            CancellationToken.None);

        result.Should().Be(Unit.Value);
        await _refreshTokenRepository.DidNotReceive()
            .UpdateAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidToken_RevokesToken()
    {
        var userId = Guid.NewGuid();
        var rawToken = "some_raw_token_value";
        var tokenHash = Sha256Hasher.ComputeHex(rawToken);
        var token = CreateActiveToken(userId, tokenHash);

        _refreshTokenRepository.FindByTokenHashAsync(tokenHash, Arg.Any<CancellationToken>()).Returns(token);

        var handler = CreateHandler();
        await handler.Handle(new LogoutCommand(rawToken, userId, "127.0.0.1"), CancellationToken.None);

        await _refreshTokenRepository.Received(1).UpdateAsync(
            Arg.Is<RefreshToken>(t => t.IsRevoked()),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AlreadyRevokedToken_ReturnsWithoutRevoking()
    {
        var userId = Guid.NewGuid();
        var rawToken = "some_raw_token_value";
        var tokenHash = Sha256Hasher.ComputeHex(rawToken);
        var token = CreateActiveToken(userId, tokenHash);
        token.Revoke();

        _refreshTokenRepository.FindByTokenHashAsync(tokenHash, Arg.Any<CancellationToken>()).Returns(token);

        var handler = CreateHandler();
        await handler.Handle(new LogoutCommand(rawToken, userId, "127.0.0.1"), CancellationToken.None);

        await _refreshTokenRepository.DidNotReceive()
            .UpdateAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TokenOwnedByDifferentUser_ThrowsForbiddenException()
    {
        var ownerUserId = Guid.NewGuid();
        var differentUserId = Guid.NewGuid();
        var rawToken = "some_raw_token_value";
        var tokenHash = Sha256Hasher.ComputeHex(rawToken);
        var token = CreateActiveToken(ownerUserId, tokenHash);

        _refreshTokenRepository.FindByTokenHashAsync(tokenHash, Arg.Any<CancellationToken>()).Returns(token);

        var handler = CreateHandler();
        var act = async () => await handler.Handle(
            new LogoutCommand(rawToken, differentUserId, "127.0.0.1"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Handle_TokenNotFound_ReturnsWithoutRevoking()
    {
        _refreshTokenRepository
            .FindByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((RefreshToken?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(
            new LogoutCommand("nonexistent_token", Guid.NewGuid(), "127.0.0.1"),
            CancellationToken.None);

        result.Should().Be(Unit.Value);
        await _refreshTokenRepository.DidNotReceive()
            .UpdateAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
    }

    private static RefreshToken CreateActiveToken(Guid userId, string tokenHash)
        => RefreshToken.Create(
            userId: userId,
            tokenHash: tokenHash,
            expiresAt: DateTime.UtcNow.AddDays(7),
            createdByIp: "127.0.0.1");
}
