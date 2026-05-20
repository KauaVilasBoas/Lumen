namespace AegisIdentity.Domain.Users;

public interface IUserRepository
{
    Task<User?> FindByEmailAsync(string email, CancellationToken ct = default);

    Task<User?> FindByIdAsync(string id, CancellationToken ct = default);

    Task<User?> FindByUsernameAsync(string username, CancellationToken ct = default);

    Task InsertAsync(User user, CancellationToken ct = default);

    Task UpdateAsync(User user, CancellationToken ct = default);
}
