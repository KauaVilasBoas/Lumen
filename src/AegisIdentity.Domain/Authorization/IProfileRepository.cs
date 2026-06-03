namespace AegisIdentity.Domain.Authorization;

public interface IProfileRepository
{
    Task<Profile?> FindByIdAsync(Guid id, CancellationToken ct = default);

    Task<Profile?> FindByNameAsync(string name, CancellationToken ct = default);

    Task<IReadOnlyList<Profile>> ListAllAsync(CancellationToken ct = default);

    Task InsertAsync(Profile profile, CancellationToken ct = default);

    Task UpdateAsync(Profile profile, CancellationToken ct = default);

    Task<HashSet<string>> GetPermissionCodesByUserIdAsync(Guid userId, CancellationToken ct = default);

    Task<bool> ActiveNameExistsAsync(string name, Guid? excludeId, CancellationToken ct = default);

    Task<IReadOnlyList<PermissionProfile>> GetPermissionProfilesByProfileIdAsync(Guid profileId, CancellationToken ct = default);

    Task<IReadOnlyList<PermissionProfile>> GetActivePermissionProfilesByProfileIdAsync(Guid profileId, CancellationToken ct = default);

    Task InsertPermissionProfilesAsync(IReadOnlyList<PermissionProfile> permissionProfiles, CancellationToken ct = default);

    Task UpdatePermissionProfileAsync(PermissionProfile permissionProfile, CancellationToken ct = default);

    Task<IReadOnlyList<Guid>> GetUserIdsByProfileIdAsync(Guid profileId, CancellationToken ct = default);

    /// <summary>
    /// Soft-deletes <paramref name="profile"/>, all of its
    /// <paramref name="permissionProfiles"/>, and all of its
    /// <paramref name="userProfiles"/> inside a single database transaction,
    /// respecting FK order (children first, then the profile itself).
    ///
    /// If any step fails the transaction is rolled back, leaving the database in
    /// the exact state it was in before the call.
    /// </summary>
    Task DeleteWithCascadeAsync(
        Profile profile,
        IReadOnlyList<PermissionProfile> permissionProfiles,
        IReadOnlyList<UserProfile> userProfiles,
        CancellationToken ct = default);
}
