using AegisIdentity.Domain.Tokens;
using AegisIdentity.SharedKernel.Constants;
using AegisIdentity.SharedKernel.Exceptions;
using AegisIdentity.SharedKernel.Util;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AegisIdentity.CommandHandlers.Auth.Logout;

public sealed class LogoutUserCommandHandler
    : IRequestHandler<LogoutUserCommandHandler.Command>
{
    public sealed record Command(string? RefreshToken, Guid UserId, string ClientIp) : IRequest;

    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ILogger<LogoutUserCommandHandler> _logger;

    public LogoutUserCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        ILogger<LogoutUserCommandHandler> logger)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _logger = logger;
    }

    public async Task Handle(Command cmd, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cmd.RefreshToken))
            return;

        var tokenHash = Sha256Hasher.ComputeHex(cmd.RefreshToken);
        var token = await _refreshTokenRepository.FindByTokenHashAsync(tokenHash, ct);

        if (token is null || IsAlreadyInactive(token))
            return;

        ValidateOwnership(token, cmd.UserId, cmd.ClientIp);

        token.Revoke();
        await _refreshTokenRepository.UpdateAsync(token, ct);

        _logger.LogInformation(
            "User {UserId} logged out successfully from {ClientIp}",
            cmd.UserId, cmd.ClientIp);
    }

    private static bool IsAlreadyInactive(RefreshToken token) =>
        token.IsRevoked() || token.IsExpired();

    private void ValidateOwnership(RefreshToken token, Guid requestingUserId, string clientIp)
    {
        if (token.UserId == requestingUserId)
            return;

        _logger.LogWarning(
            "Refresh token ownership violation — requestingUserId: {RequestingUserId}, tokenOwnerId: {TokenOwnerId}, clientIp: {ClientIp}",
            requestingUserId, token.UserId, clientIp);

        throw new ForbiddenException(AuthErrorMessages.RefreshTokenOwnershipViolation);
    }
}
