using Lumen.Identity.Domain.Tokens;

namespace Lumen.Identity.Application.Tokens;

internal sealed class TokenCleanupService : ITokenCleanupService
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public TokenCleanupService(IRefreshTokenRepository refreshTokenRepository)
    {
        _refreshTokenRepository = refreshTokenRepository;
    }

    public Task<int> DeleteExpiredRefreshTokensAsync(DateTime cutoff, CancellationToken ct = default)
        => _refreshTokenRepository.DeleteExpiredAsync(cutoff, ct);
}
