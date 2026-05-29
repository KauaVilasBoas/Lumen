using System.Reflection;
using AegisIdentity.CommandHandlers.Auth.Refresh;
using AegisIdentity.Domain.Configuration;
using AegisIdentity.Domain.Security;
using AegisIdentity.Domain.Tokens;
using AegisIdentity.Domain.Users;
using AegisIdentity.SharedKernel.Constants;
using AegisIdentity.SharedKernel.Exceptions;
using AegisIdentity.SharedKernel.Util;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

namespace AegisIdentity.UnitTests.Application.Auth.Refresh;

public sealed class RefreshTokenCommandHandlerTests
{
    private const string ValidEmail = "bob@example.com";
    private const string ValidUsername = "bob";
    private const string FakePasswordHash = "$2a$12$fakehash";
    private const string FakeAccessToken = "header.payload.signature";
    private const string FakeNewRefreshTokenValue = "new-opaque-refresh-token";
    private const string ClientIp = "10.0.0.1";
    private const string SomeIncomingTokenValue = "some-refresh-token-value";

    private readonly IRefreshTokenRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IJwtService _jwtService = Substitute.For<IJwtService>();
    private readonly IAppSettings _appSettings = Substitute.For<IAppSettings>();
    private readonly ILogger<RefreshTokenCommandHandler> _logger = Substitute.For<ILogger<RefreshTokenCommandHandler>>();

    public RefreshTokenCommandHandlerTests()
    {
        _appSettings.RefreshTokenExpirationDays.Returns(7);
        _jwtService.GenerateAccessToken(Arg.Any<User>()).Returns(FakeAccessToken);
        _jwtService.GenerateRefreshTokenValue().Returns(FakeNewRefreshTokenValue);
        _jwtService.AccessTokenExpiresIn.Returns(900);
    }

    [Fact]
    public async Task Handle_WithValidTokenAndActiveUser_ReturnsNewTokenPairWithBearerType()
    {
        var user = ActiveUser();
        var (incomingValue, incomingToken) = ActiveTokenForUser(user.Id);

        ArrangeFoundToken(incomingValue, incomingToken);
        _userRepository.FindByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var result = await CreateHandler().Handle(CommandWith(incomingValue), CancellationToken.None);

        result.AccessToken.Should().Be(FakeAccessToken);
        result.RefreshToken.Should().Be(FakeNewRefreshTokenValue);
        result.TokenType.Should().Be(TokenTypes.Bearer);
        result.ExpiresIn.Should().Be(900);
    }

    [Fact]
    public async Task Handle_WithValidTokenAndActiveUser_RevokesOldTokenWithNewHash()
    {
        var user = ActiveUser();
        var (incomingValue, incomingToken) = ActiveTokenForUser(user.Id);

        ArrangeFoundToken(incomingValue, incomingToken);
        _userRepository.FindByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        await CreateHandler().Handle(CommandWith(incomingValue), CancellationToken.None);

        var expectedNewHash = Sha256Hasher.ComputeHex(FakeNewRefreshTokenValue);
        incomingToken.IsRevoked().Should().BeTrue();
        incomingToken.ReplacedByTokenHash.Should().Be(expectedNewHash);

        await _refreshTokenRepository.Received(1).UpdateAsync(incomingToken, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithValidTokenAndActiveUser_InsertsNewRefreshTokenLinkedToUser()
    {
        var user = ActiveUser();
        var (incomingValue, incomingToken) = ActiveTokenForUser(user.Id);

        ArrangeFoundToken(incomingValue, incomingToken);
        _userRepository.FindByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        await CreateHandler().Handle(CommandWith(incomingValue), CancellationToken.None);

        await _refreshTokenRepository.Received(1).InsertAsync(
            Arg.Is<RefreshToken>(t => t.UserId == user.Id && t.CreatedByIp == ClientIp),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTokenNotFound_ThrowsUnauthorizedWithGenericMessage()
    {
        _refreshTokenRepository.FindByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ReturnsNull();

        var act = () => CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        var exception = await act.Should().ThrowAsync<UnauthorizedException>();
        exception.Which.Message.Should().Be(AuthErrorMessages.InvalidOrExpiredRefreshToken);
    }

    [Fact]
    public async Task Handle_WhenTokenIsExpired_ThrowsUnauthorizedWithGenericMessage()
    {
        var user = ActiveUser();
        var expiredToken = ExpiredTokenFor(user.Id);

        _refreshTokenRepository.FindByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(expiredToken);

        var act = () => CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        var exception = await act.Should().ThrowAsync<UnauthorizedException>();
        exception.Which.Message.Should().Be(AuthErrorMessages.InvalidOrExpiredRefreshToken);
    }

    [Fact]
    public async Task Handle_WhenTokenIsRevokedAndAlsoExpired_ThrowsUnauthorizedWithoutWalkingChain()
    {
        var user = ActiveUser();
        var expiredRevokedToken = ExpiredTokenFor(user.Id);
        expiredRevokedToken.Revoke("some-child-hash");

        _refreshTokenRepository.FindByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(expiredRevokedToken);

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => CreateHandler().Handle(ValidCommand(), CancellationToken.None));

        await _refreshTokenRepository.DidNotReceive().UpdateAsync(
            Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenReplayDetected_RevokesDirectDescendantInChain()
    {
        var user = ActiveUser();
        var (incomingValue, revokedToken) = RevokedTokenWithChildFor(user.Id, childHash: "child-hash");
        var child = ActiveTokenFor(user.Id, tokenHash: "child-hash", childHash: null);

        ArrangeFoundToken(incomingValue, revokedToken);
        _refreshTokenRepository.FindByTokenHashAsync("child-hash", Arg.Any<CancellationToken>())
            .Returns(child);

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => CreateHandler().Handle(CommandWith(incomingValue), CancellationToken.None));

        child.IsRevoked().Should().BeTrue("descendant token in the chain must be revoked on replay detection");
        await _refreshTokenRepository.Received(1).UpdateAsync(child, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenReplayDetected_RevokesMultipleDescendantsViaChainWalk()
    {
        var user = ActiveUser();
        var (incomingValue, revokedToken) = RevokedTokenWithChildFor(user.Id, childHash: "child-hash");
        var child = ActiveTokenFor(user.Id, tokenHash: "child-hash", childHash: "grandchild-hash");
        var grandchild = ActiveTokenFor(user.Id, tokenHash: "grandchild-hash", childHash: null);

        ArrangeFoundToken(incomingValue, revokedToken);
        _refreshTokenRepository.FindByTokenHashAsync("child-hash", Arg.Any<CancellationToken>())
            .Returns(child);
        _refreshTokenRepository.FindByTokenHashAsync("grandchild-hash", Arg.Any<CancellationToken>())
            .Returns(grandchild);

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => CreateHandler().Handle(CommandWith(incomingValue), CancellationToken.None));

        child.IsRevoked().Should().BeTrue("child token must be revoked during chain walk");
        grandchild.IsRevoked().Should().BeTrue("grandchild token must be revoked during chain walk");

        await _refreshTokenRepository.Received(1).UpdateAsync(child, Arg.Any<CancellationToken>());
        await _refreshTokenRepository.Received(1).UpdateAsync(grandchild, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenReplayDetected_StopsChainWalkWhenDescendantNotFound()
    {
        var user = ActiveUser();
        var (incomingValue, revokedToken) = RevokedTokenWithChildFor(user.Id, childHash: "missing-hash");

        ArrangeFoundToken(incomingValue, revokedToken);
        _refreshTokenRepository.FindByTokenHashAsync("missing-hash", Arg.Any<CancellationToken>())
            .ReturnsNull();

        var act = () => CreateHandler().Handle(CommandWith(incomingValue), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>("replay must still be rejected even when descendant is not found");
        await _refreshTokenRepository.DidNotReceive().UpdateAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenReplayDetected_EmitsLogWarningWithUserIdAndClientIp()
    {
        var user = ActiveUser();
        var (incomingValue, revokedToken) = RevokedTokenWithChildFor(user.Id, childHash: null);

        ArrangeFoundToken(incomingValue, revokedToken);

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => CreateHandler().Handle(CommandWith(incomingValue), CancellationToken.None));

        _logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(v => v.ToString()!.Contains(user.Id) && v.ToString()!.Contains(ClientIp)),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task Handle_WhenUserIsInactive_ThrowsUnauthorizedWithGenericMessage()
    {
        var inactiveUser = InactiveUser();
        var (incomingValue, activeToken) = ActiveTokenForUser(inactiveUser.Id);

        ArrangeFoundToken(incomingValue, activeToken);
        _userRepository.FindByIdAsync(inactiveUser.Id, Arg.Any<CancellationToken>()).Returns(inactiveUser);

        var act = () => CreateHandler().Handle(CommandWith(incomingValue), CancellationToken.None);

        var exception = await act.Should().ThrowAsync<UnauthorizedException>();
        exception.Which.Message.Should().Be(AuthErrorMessages.InvalidOrExpiredRefreshToken);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ThrowsUnauthorizedWithGenericMessage()
    {
        var (incomingValue, orphanToken) = ActiveTokenForUser("orphan-user-id");

        ArrangeFoundToken(incomingValue, orphanToken);
        _userRepository.FindByIdAsync("orphan-user-id", Arg.Any<CancellationToken>())
            .ReturnsNull();

        var act = () => CreateHandler().Handle(CommandWith(incomingValue), CancellationToken.None);

        var exception = await act.Should().ThrowAsync<UnauthorizedException>();
        exception.Which.Message.Should().Be(AuthErrorMessages.InvalidOrExpiredRefreshToken);
    }

    private RefreshTokenCommandHandler CreateHandler() =>
        new(
            _refreshTokenRepository,
            _userRepository,
            _jwtService,
            _appSettings,
            _logger);

    private static RefreshTokenCommandHandler.Command ValidCommand() =>
        CommandWith(SomeIncomingTokenValue);

    private static RefreshTokenCommandHandler.Command CommandWith(string tokenValue) =>
        new(RefreshToken: tokenValue, ClientIp: ClientIp);

    private static User ActiveUser()
    {
        var user = User.Create(ValidEmail, ValidUsername, FakePasswordHash);
        user.IsActive = true;
        return user;
    }

    private static User InactiveUser() =>
        User.Create(ValidEmail, ValidUsername, FakePasswordHash);

    private static (string TokenValue, RefreshToken Token) ActiveTokenForUser(string userId)
    {
        var value = SomeIncomingTokenValue;
        var hash = Sha256Hasher.ComputeHex(value);
        var token = RefreshToken.Create(
            userId: userId,
            tokenHash: hash,
            expiresAt: DateTime.UtcNow.AddDays(7),
            createdByIp: ClientIp);
        return (value, token);
    }

    private static RefreshToken ActiveTokenFor(string userId, string tokenHash, string? childHash)
    {
        if (childHash is not null)
            return TokenWithHashAndChild(userId, tokenHash, childHash);

        return RefreshToken.Create(
            userId: userId,
            tokenHash: tokenHash,
            expiresAt: DateTime.UtcNow.AddDays(7),
            createdByIp: ClientIp);
    }

    private static RefreshToken TokenWithHashAndChild(string userId, string tokenHash, string childHash)
    {
        var token = (RefreshToken)System.Runtime.CompilerServices.RuntimeHelpers
            .GetUninitializedObject(typeof(RefreshToken));

        SetInitProperty(token, nameof(RefreshToken.Id), Guid.NewGuid().ToString());
        SetInitProperty(token, nameof(RefreshToken.UserId), userId);
        SetInitProperty(token, nameof(RefreshToken.TokenHash), tokenHash);
        SetInitProperty(token, nameof(RefreshToken.CreatedByIp), ClientIp);
        SetInitProperty(token, nameof(RefreshToken.ExpiresAt), DateTime.UtcNow.AddDays(7));
        SetInitProperty(token, nameof(RefreshToken.CreatedAt), DateTime.UtcNow);

        SetPrivateField(token, "<ReplacedByTokenHash>k__BackingField", childHash);

        return token;
    }

    private static (string TokenValue, RefreshToken Token) RevokedTokenWithChildFor(string userId, string? childHash)
    {
        var value = SomeIncomingTokenValue;
        var hash = Sha256Hasher.ComputeHex(value);
        var token = RefreshToken.Create(
            userId: userId,
            tokenHash: hash,
            expiresAt: DateTime.UtcNow.AddDays(7),
            createdByIp: ClientIp);
        token.Revoke(childHash);
        return (value, token);
    }

    private static RefreshToken ExpiredTokenFor(string userId)
    {
        var tokenHash = Sha256Hasher.ComputeHex(SomeIncomingTokenValue);

        var token = (RefreshToken)System.Runtime.CompilerServices.RuntimeHelpers
            .GetUninitializedObject(typeof(RefreshToken));

        SetInitProperty(token, nameof(RefreshToken.Id), Guid.NewGuid().ToString());
        SetInitProperty(token, nameof(RefreshToken.UserId), userId);
        SetInitProperty(token, nameof(RefreshToken.TokenHash), tokenHash);
        SetInitProperty(token, nameof(RefreshToken.CreatedByIp), ClientIp);
        SetInitProperty(token, nameof(RefreshToken.ExpiresAt), DateTime.UtcNow.AddDays(-1));
        SetInitProperty(token, nameof(RefreshToken.CreatedAt), DateTime.UtcNow);

        return token;
    }

    private static void SetInitProperty(object target, string propertyName, object value) =>
        SetPrivateField(target, $"<{propertyName}>k__BackingField", value);

    private static void SetPrivateField(object target, string fieldName, object? value)
    {
        var field = target.GetType()
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);

        field?.SetValue(target, value);
    }

    private void ArrangeFoundToken(string tokenValue, RefreshToken token)
    {
        var hash = Sha256Hasher.ComputeHex(tokenValue);
        _refreshTokenRepository.FindByTokenHashAsync(hash, Arg.Any<CancellationToken>())
            .Returns(token);
    }
}
