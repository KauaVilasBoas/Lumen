namespace AegisIdentity.Domain.Tokens;

/// <summary>
/// Repository abstraction for <see cref="EmailConfirmationToken"/> persistence.
///
/// Defined in the Domain layer so Application and Domain use-cases depend only on this
/// interface (Dependency Inversion). The concrete MongoDB implementation lives in
/// Infrastructure and is wired at composition root.
/// </summary>
public interface IEmailConfirmationTokenRepository
{
    /// <summary>Finds an email confirmation token by its SHA-256 hash. Returns null when not found.</summary>
    Task<EmailConfirmationToken?> FindByTokenHashAsync(string tokenHash, CancellationToken ct = default);

    /// <summary>Inserts a new email confirmation token document.</summary>
    Task InsertAsync(EmailConfirmationToken token, CancellationToken ct = default);

    /// <summary>Replaces the full email confirmation token document identified by <see cref="EmailConfirmationToken.Id"/>.</summary>
    Task UpdateAsync(EmailConfirmationToken token, CancellationToken ct = default);
}
