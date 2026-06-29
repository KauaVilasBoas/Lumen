namespace Lumen.Modules.Identity.Application.Tokens;

public interface ITokenCleanupService
{
    Task<int> DeleteExpiredRefreshTokensAsync(DateTime cutoff, CancellationToken ct = default);
}
