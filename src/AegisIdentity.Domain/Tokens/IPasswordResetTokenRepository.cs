namespace AegisIdentity.Domain.Tokens;

/// <summary>
/// Repository abstraction for <see cref="PasswordResetToken"/> persistence.
///
/// Defined in the Domain layer so Application and Domain use-cases depend only on this
/// interface (Dependency Inversion). The concrete MongoDB implementation lives in
/// Infrastructure and is wired at composition root.
/// </summary>
public interface IPasswordResetTokenRepository
{
    /// <summary>Finds a password reset token by its SHA-256 hash. Returns null when not found.</summary>
    Task<PasswordResetToken?> FindByTokenHashAsync(string tokenHash, CancellationToken ct = default);

    /// <summary>Inserts a new password reset token document.</summary>
    Task InsertAsync(PasswordResetToken token, CancellationToken ct = default);

    /// <summary>Replaces the full password reset token document identified by <see cref="PasswordResetToken.Id"/>.</summary>
    Task UpdateAsync(PasswordResetToken token, CancellationToken ct = default);
}
