namespace AegisIdentity.Domain.Tokens;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> FindByTokenHashAsync(string tokenHash, CancellationToken ct = default);

    Task<IReadOnlyList<RefreshToken>> FindByUserIdAsync(string userId, CancellationToken ct = default);

    Task InsertAsync(RefreshToken token, CancellationToken ct = default);

    Task UpdateAsync(RefreshToken token, CancellationToken ct = default);

    /// <summary>
    /// Deletes all refresh tokens whose <see cref="RefreshToken.ExpiresAt"/> is
    /// earlier than <paramref name="cutoff"/>.  Called by the scheduled cleanup job.
    /// </summary>
    /// <returns>The number of documents deleted.</returns>
    Task<long> DeleteExpiredAsync(DateTime cutoff, CancellationToken ct = default);
}
