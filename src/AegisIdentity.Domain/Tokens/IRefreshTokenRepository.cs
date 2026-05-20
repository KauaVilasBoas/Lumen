namespace AegisIdentity.Domain.Tokens;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> FindByTokenHashAsync(string tokenHash, CancellationToken ct = default);

    Task<IReadOnlyList<RefreshToken>> FindByUserIdAsync(string userId, CancellationToken ct = default);

    Task InsertAsync(RefreshToken token, CancellationToken ct = default);

    Task UpdateAsync(RefreshToken token, CancellationToken ct = default);
}
