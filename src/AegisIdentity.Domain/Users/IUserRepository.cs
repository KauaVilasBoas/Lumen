namespace AegisIdentity.Domain.Users;

public interface IUserRepository
{
    Task<User?> FindByEmailAsync(string email, CancellationToken ct = default);

    Task<User?> FindByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Finds a user by <paramref name="id"/> bypassing the global soft-delete query filter,
    /// so that deleted users are also returned.
    /// </summary>
    Task<User?> FindByIdIgnoringFiltersAsync(Guid id, CancellationToken ct = default);

    Task<User?> FindByUsernameAsync(string username, CancellationToken ct = default);

    Task InsertAsync(User user, CancellationToken ct = default);

    Task UpdateAsync(User user, CancellationToken ct = default);

    /// <summary>
    /// Returns a page of users matching the given filters.
    /// When <paramref name="includeDeleted"/> is <c>true</c> the global soft-delete
    /// query filter is bypassed so deleted users are also considered.
    /// </summary>
    Task<(IReadOnlyList<User> Items, int Total)> ListAsync(
        string? search,
        bool includeDeleted,
        int page,
        int pageSize,
        CancellationToken ct = default);
}
