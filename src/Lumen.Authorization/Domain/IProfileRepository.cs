namespace Lumen.Authorization.Domain;

public interface IProfileRepository
{
    Task<Profile?> FindByIdAsync(Guid id, CancellationToken ct = default);

    Task<Profile?> FindByNameAsync(string name, CancellationToken ct = default);

    Task<IReadOnlyList<Profile>> ListAllAsync(CancellationToken ct = default);

    Task<IReadOnlyList<Profile>> GetProfilesByUserIdAsync(Guid userId, CancellationToken ct = default);

    Task<IReadOnlyDictionary<Guid, IReadOnlyList<Profile>>> GetProfilesByUserIdsAsync(
        IReadOnlyList<Guid> userIds,
        CancellationToken ct = default);

    Task<IReadOnlyDictionary<Guid, int>> GetPermissionCountsByUserIdsAsync(
        IReadOnlyList<Guid> userIds,
        CancellationToken ct = default);

    Task<IReadOnlyDictionary<Guid, int>> GetPermissionCountsByProfileIdsAsync(
        IReadOnlyList<Guid> profileIds,
        CancellationToken ct = default);

    Task<IReadOnlyList<Profile>> GetByIdsAsync(IReadOnlyList<Guid> ids, CancellationToken ct = default);

    Task InsertAsync(Profile profile, CancellationToken ct = default);

    Task UpdateAsync(Profile profile, CancellationToken ct = default);

    Task<HashSet<string>> GetPermissionCodesByUserIdAsync(Guid userId, CancellationToken ct = default);

    Task<bool> ActiveNameExistsAsync(string name, Guid? excludeId, CancellationToken ct = default);

    Task<IReadOnlyList<PermissionProfile>> GetPermissionProfilesByProfileIdAsync(Guid profileId, CancellationToken ct = default);

    Task<IReadOnlyList<PermissionProfile>> GetActivePermissionProfilesByProfileIdAsync(Guid profileId, CancellationToken ct = default);

    Task<IReadOnlyDictionary<Guid, IReadOnlyList<PermissionProfile>>> GetActivePermissionProfilesByProfileIdsAsync(
        IReadOnlyList<Guid> profileIds,
        CancellationToken ct = default);

    Task InsertPermissionProfilesAsync(IReadOnlyList<PermissionProfile> permissionProfiles, CancellationToken ct = default);

    Task UpdatePermissionProfileAsync(PermissionProfile permissionProfile, CancellationToken ct = default);

    Task<IReadOnlyList<Guid>> GetUserIdsByProfileIdAsync(Guid profileId, CancellationToken ct = default);

    Task DeleteWithCascadeAsync(
        Profile profile,
        IReadOnlyList<PermissionProfile> permissionProfiles,
        IReadOnlyList<UserProfile> userProfiles,
        CancellationToken ct = default);
}
