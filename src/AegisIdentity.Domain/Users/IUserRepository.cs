namespace AegisIdentity.Domain.Users;

/// <summary>
/// Repository abstraction for <see cref="User"/> persistence.
///
/// Defined in the Domain layer so Application and Domain use-cases depend only on this
/// interface (Dependency Inversion). The concrete MongoDB implementation lives in
/// Infrastructure and is wired at composition root.
///
/// All methods accept a <see cref="CancellationToken"/> to support graceful cancellation
/// from API request pipelines and background services.
/// </summary>
public interface IUserRepository
{
    /// <summary>Finds a user by their normalised email address. Returns null when not found.</summary>
    Task<User?> FindByEmailAsync(string email, CancellationToken ct = default);

    /// <summary>Finds a user by their MongoDB ObjectId string. Returns null when not found.</summary>
    Task<User?> FindByIdAsync(string id, CancellationToken ct = default);

    /// <summary>Finds a user by their username (case-sensitive). Returns null when not found.</summary>
    Task<User?> FindByUsernameAsync(string username, CancellationToken ct = default);

    /// <summary>Inserts a new user document. Throws on duplicate email or username.</summary>
    Task InsertAsync(User user, CancellationToken ct = default);

    /// <summary>Replaces the full user document identified by <see cref="User.Id"/>.</summary>
    Task UpdateAsync(User user, CancellationToken ct = default);
}
