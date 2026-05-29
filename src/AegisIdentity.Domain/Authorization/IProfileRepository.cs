namespace AegisIdentity.Domain.Authorization;

public interface IProfileRepository
{
    Task<Profile?> FindByIdAsync(Guid id, CancellationToken ct = default);

    Task<Profile?> FindByNameAsync(string name, CancellationToken ct = default);

    Task<IReadOnlyList<Profile>> ListAllAsync(CancellationToken ct = default);

    Task InsertAsync(Profile profile, CancellationToken ct = default);

    Task UpdateAsync(Profile profile, CancellationToken ct = default);

    Task<HashSet<string>> GetPermissionCodesByUserIdAsync(Guid userId, CancellationToken ct = default);
}
