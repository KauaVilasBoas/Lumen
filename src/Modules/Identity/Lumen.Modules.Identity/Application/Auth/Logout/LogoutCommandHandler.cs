using Lumen.Modules.Identity.Domain.Tokens;
using Lumen.SharedKernel.Constants;
using Lumen.SharedKernel.Exceptions;
using Lumen.SharedKernel.Util;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lumen.Modules.Identity.Application.Auth.Logout;

internal sealed class LogoutCommandHandler
    : IRequestHandler<LogoutCommandHandler.Command, Unit>
{
    public sealed record Command(string? RefreshToken, Guid UserId, string ClientIp) : IRequest<Unit>;

    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ILogger<LogoutCommandHandler> _logger;

    public LogoutCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        ILogger<LogoutCommandHandler> logger)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _logger = logger;
    }

    public async Task<Unit> Handle(Command cmd, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cmd.RefreshToken))
            return Unit.Value;

        var tokenHash = Sha256Hasher.ComputeHex(cmd.RefreshToken);
        var token = await _refreshTokenRepository.FindByTokenHashAsync(tokenHash, ct);

        if (token is null || IsAlreadyInactive(token))
            return Unit.Value;

        ValidateOwnership(token, cmd.UserId, cmd.ClientIp);

        token.Revoke();
        await _refreshTokenRepository.UpdateAsync(token, ct);

        _logger.LogInformation(
            "User {UserId} logged out successfully from {ClientIp}",
            cmd.UserId, cmd.ClientIp);

        return Unit.Value;
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
