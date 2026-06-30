using FluentValidation;
using Lumen.Modules.Identity.Domain.Configuration;
using Lumen.Modules.Identity.Domain.Security;
using Lumen.Modules.Identity.Domain.Tokens;
using Lumen.Modules.Identity.Domain.Users;
using Lumen.SharedKernel.Constants;
using Lumen.SharedKernel.Exceptions;
using Lumen.SharedKernel.Util;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lumen.Modules.Identity.Application.Auth.Refresh;

public sealed record RefreshTokenCommand(string RefreshToken, string ClientIp) : IRequest<RefreshTokenResult>;

public sealed record RefreshTokenResult(string AccessToken, string RefreshToken, int ExpiresIn, string TokenType = TokenTypes.Bearer);

internal sealed class RefreshTokenCommandHandler
    : IRequestHandler<RefreshTokenCommand, RefreshTokenResult>
{
    public sealed class Validator : AbstractValidator<RefreshTokenCommand>
    {
        public Validator()
        {
            RuleFor(x => x.RefreshToken)
                .NotEmpty().WithMessage("O campo refresh token é obrigatório.");
        }
    }

    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly IJwtService _jwtService;
    private readonly IAppSettings _appSettings;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    public RefreshTokenCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IUserRepository userRepository,
        IJwtService jwtService,
        IAppSettings appSettings,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _userRepository = userRepository;
        _jwtService = jwtService;
        _appSettings = appSettings;
        _logger = logger;
    }

    public async Task<RefreshTokenResult> Handle(RefreshTokenCommand cmd, CancellationToken ct)
    {
        var incomingHash = Sha256Hasher.ComputeHex(cmd.RefreshToken);

        var token = await _refreshTokenRepository.FindByTokenHashAsync(incomingHash, ct)
            ?? throw new UnauthorizedException(AuthErrorMessages.InvalidOrExpiredRefreshToken);

        if (token.IsExpired())
            throw new UnauthorizedException(AuthErrorMessages.InvalidOrExpiredRefreshToken);

        if (token.IsRevoked())
        {
            await HandleReplayDetectedAsync(token, cmd.ClientIp, ct);
            throw new UnauthorizedException(AuthErrorMessages.InvalidOrExpiredRefreshToken);
        }

        var user = await _userRepository.FindByIdAsync(token.UserId, ct);

        if (user is null || !user.IsActive)
            throw new UnauthorizedException(AuthErrorMessages.InvalidOrExpiredRefreshToken);

        return await RotateTokenAsync(token, user, cmd.ClientIp, ct);
    }

    private async Task HandleReplayDetectedAsync(RefreshToken revokedToken, string clientIp, CancellationToken ct)
    {
        _logger.LogWarning(
            "Refresh token replay detected — userId: {UserId}, clientIp: {ClientIp}",
            revokedToken.UserId, clientIp);

        await RevokeChainAsync(revokedToken.ReplacedByTokenHash, ct);
    }

    private async Task RevokeChainAsync(string? startingReplacedByHash, CancellationToken ct)
    {
        var nextHash = startingReplacedByHash;

        while (nextHash is not null)
        {
            var next = await _refreshTokenRepository.FindByTokenHashAsync(nextHash, ct);

            if (next is null)
                break;

            var followHash = next.ReplacedByTokenHash;

            if (!next.IsRevoked())
            {
                next.Revoke();
                await _refreshTokenRepository.UpdateAsync(next, ct);
            }

            nextHash = followHash;
        }
    }

    private async Task<RefreshTokenResult> RotateTokenAsync(RefreshToken current, User user, string clientIp, CancellationToken ct)
    {
        var newTokenValue = _jwtService.GenerateRefreshTokenValue();
        var newTokenHash = Sha256Hasher.ComputeHex(newTokenValue);

        current.Revoke(newTokenHash);
        await _refreshTokenRepository.UpdateAsync(current, ct);

        var newToken = RefreshToken.Create(
            userId: user.Id,
            tokenHash: newTokenHash,
            expiresAt: DateTime.UtcNow.AddDays(_appSettings.RefreshTokenExpirationDays),
            createdByIp: clientIp);

        await _refreshTokenRepository.InsertAsync(newToken, ct);

        var accessToken = _jwtService.GenerateAccessToken(user);

        return new RefreshTokenResult(accessToken, newTokenValue, _jwtService.AccessTokenExpiresIn);
    }
}
