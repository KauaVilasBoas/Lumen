namespace AegisIdentity.Domain.Tokens;

/// <summary>
/// Repository abstraction for <see cref="RefreshToken"/> persistence.
///
/// Defined in the Domain layer so Application and Domain use-cases depend only on this
/// interface (Dependency Inversion). The concrete MongoDB implementation lives in
/// Infrastructure and is wired at composition root.
/// </summary>
public interface IRefreshTokenRepository
{
    /// <summary>Finds a refresh token by its SHA-256 hash. Returns null when not found.</summary>
    Task<RefreshToken?> FindByTokenHashAsync(string tokenHash, CancellationToken ct = default);

    /// <summary>Returns all refresh tokens belonging to the given user.</summary>
    Task<IReadOnlyList<RefreshToken>> FindByUserIdAsync(string userId, CancellationToken ct = default);

    /// <summary>Inserts a new refresh token document.</summary>
    Task InsertAsync(RefreshToken token, CancellationToken ct = default);

    /// <summary>Replaces the full refresh token document identified by <see cref="RefreshToken.Id"/>.</summary>
    Task UpdateAsync(RefreshToken token, CancellationToken ct = default);
}
