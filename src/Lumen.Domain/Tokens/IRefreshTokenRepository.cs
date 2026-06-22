namespace Lumen.Domain.Tokens;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> FindByTokenHashAsync(string tokenHash, CancellationToken ct = default);

    Task<IReadOnlyList<RefreshToken>> FindByUserIdAsync(Guid userId, CancellationToken ct = default);

    Task InsertAsync(RefreshToken token, CancellationToken ct = default);

    Task UpdateAsync(RefreshToken token, CancellationToken ct = default);

    Task<long> DeleteExpiredAsync(DateTime cutoff, CancellationToken ct = default);
}
