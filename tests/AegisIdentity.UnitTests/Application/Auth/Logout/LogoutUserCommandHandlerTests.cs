using System.Reflection;
using AegisIdentity.CommandHandlers.Auth.Logout;
using AegisIdentity.Domain.Tokens;
using AegisIdentity.SharedKernel.Constants;
using AegisIdentity.SharedKernel.Exceptions;
using AegisIdentity.SharedKernel.Util;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

namespace AegisIdentity.UnitTests.Application.Auth.Logout;

public sealed class LogoutUserCommandHandlerTests
{
    private const string OwnerUserId = "owner-user-id";
    private const string OtherUserId = "other-user-id";
    private const string ClientIp = "192.168.0.1";
    private const string SomeTokenValue = "some-opaque-refresh-token";

    private readonly IRefreshTokenRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
    private readonly ILogger<LogoutUserCommandHandler> _logger = Substitute.For<ILogger<LogoutUserCommandHandler>>();

    [Fact]
    public async Task Handle_WhenRefreshTokenIsNull_CompletesWithoutCallingRepository()
    {
        var command = new LogoutUserCommandHandler.Command(
            RefreshToken: null,
            UserId: OwnerUserId,
            ClientIp: ClientIp);

        await CreateHandler().Handle(command, CancellationToken.None);

        await _refreshTokenRepository.DidNotReceive()
            .FindByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenRefreshTokenIsEmpty_CompletesWithoutCallingRepository()
    {
        var command = new LogoutUserCommandHandler.Command(
            RefreshToken: string.Empty,
            UserId: OwnerUserId,
            ClientIp: ClientIp);

        await CreateHandler().Handle(command, CancellationToken.None);

        await _refreshTokenRepository.DidNotReceive()
            .FindByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTokenNotFoundInRepository_CompletesWithoutCallingUpdateAsync()
    {
        _refreshTokenRepository
            .FindByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ReturnsNull();

        await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        await _refreshTokenRepository.DidNotReceive()
            .UpdateAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTokenIsAlreadyRevoked_CompletesWithoutCallingUpdateAsync()
    {
        var token = RevokedTokenForOwner();
        ArrangeFoundToken(token);

        await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        await _refreshTokenRepository.DidNotReceive()
            .UpdateAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTokenIsExpiredForOwner_CompletesWithoutCallingUpdateAsync()
    {
        var token = ExpiredTokenForOwner();
        ArrangeFoundToken(token);

        await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        await _refreshTokenRepository.DidNotReceive()
            .UpdateAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTokenBelongsToOtherUser_ThrowsForbiddenException()
    {
        var token = ActiveTokenForUser(OtherUserId);
        ArrangeFoundToken(token);

        var act = () => CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        var exception = await act.Should().ThrowAsync<ForbiddenException>();
        exception.Which.Message.Should().Be(AuthErrorMessages.RefreshTokenOwnershipViolation);
    }

    [Fact]
    public async Task Handle_WhenTokenBelongsToOtherUser_EmitsLogWarningWithUserIds()
    {
        var token = ActiveTokenForUser(OtherUserId);
        ArrangeFoundToken(token);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => CreateHandler().Handle(ValidCommand(), CancellationToken.None));

        _logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(v =>
                v.ToString()!.Contains(OwnerUserId) &&
                v.ToString()!.Contains(OtherUserId) &&
                v.ToString()!.Contains(ClientIp)),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task Handle_WhenTokenBelongsToOtherUser_DoesNotCallUpdateAsync()
    {
        var token = ActiveTokenForUser(OtherUserId);
        ArrangeFoundToken(token);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => CreateHandler().Handle(ValidCommand(), CancellationToken.None));

        await _refreshTokenRepository.DidNotReceive()
            .UpdateAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithValidOwnerToken_RevokesTokenAndCallsUpdateAsync()
    {
        var token = ActiveTokenForUser(OwnerUserId);
        ArrangeFoundToken(token);

        await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        token.IsRevoked().Should().BeTrue();
        await _refreshTokenRepository.Received(1)
            .UpdateAsync(token, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithValidOwnerToken_EmitsLogInformationWithUserIdAndClientIp()
    {
        var token = ActiveTokenForUser(OwnerUserId);
        ArrangeFoundToken(token);

        await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        _logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(v =>
                v.ToString()!.Contains(OwnerUserId) &&
                v.ToString()!.Contains(ClientIp)),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    private LogoutUserCommandHandler CreateHandler() =>
        new(_refreshTokenRepository, _logger);

    private static LogoutUserCommandHandler.Command ValidCommand() =>
        new(RefreshToken: SomeTokenValue, UserId: OwnerUserId, ClientIp: ClientIp);

    private static RefreshToken ActiveTokenForUser(string userId)
    {
        var hash = Sha256Hasher.ComputeHex(SomeTokenValue);
        return RefreshToken.Create(
            userId: userId,
            tokenHash: hash,
            expiresAt: DateTime.UtcNow.AddDays(7),
            createdByIp: ClientIp);
    }

    private static RefreshToken RevokedTokenForOwner()
    {
        var token = ActiveTokenForUser(OwnerUserId);
        token.Revoke();
        return token;
    }

    private static RefreshToken ExpiredTokenForOwner()
    {
        var token = (RefreshToken)System.Runtime.CompilerServices.RuntimeHelpers
            .GetUninitializedObject(typeof(RefreshToken));

        SetInitProperty(token, nameof(RefreshToken.Id), Guid.NewGuid().ToString());
        SetInitProperty(token, nameof(RefreshToken.UserId), OwnerUserId);
        SetInitProperty(token, nameof(RefreshToken.TokenHash), Sha256Hasher.ComputeHex(SomeTokenValue));
        SetInitProperty(token, nameof(RefreshToken.CreatedByIp), ClientIp);
        SetInitProperty(token, nameof(RefreshToken.ExpiresAt), DateTime.UtcNow.AddDays(-1));
        SetInitProperty(token, nameof(RefreshToken.CreatedAt), DateTime.UtcNow);

        return token;
    }

    private void ArrangeFoundToken(RefreshToken token)
    {
        var hash = Sha256Hasher.ComputeHex(SomeTokenValue);
        _refreshTokenRepository
            .FindByTokenHashAsync(hash, Arg.Any<CancellationToken>())
            .Returns(token);
    }

    private static void SetInitProperty(object target, string propertyName, object value) =>
        SetPrivateField(target, $"<{propertyName}>k__BackingField", value);

    private static void SetPrivateField(object target, string fieldName, object? value)
    {
        var field = target.GetType()
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);

        field?.SetValue(target, value);
    }
}
