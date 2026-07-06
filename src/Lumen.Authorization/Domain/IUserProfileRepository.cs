namespace Lumen.Authorization.Domain;

public interface IUserProfileRepository
{
    Task<UserProfile?> FindActiveAsync(Guid userId, Guid profileId, Guid? scopeId = null, CancellationToken ct = default);

    Task<IReadOnlyList<UserProfile>> ListByUserIdAsync(Guid userId, CancellationToken ct = default);

    Task<IReadOnlyDictionary<Guid, IReadOnlyList<UserProfile>>> ListByUserIdsAsync(
        IReadOnlyList<Guid> userIds,
        CancellationToken ct = default);

    Task<IReadOnlyList<UserProfile>> ListByProfileIdAsync(Guid profileId, CancellationToken ct = default);

    Task InsertAsync(UserProfile userProfile, CancellationToken ct = default);

    Task UpdateAsync(UserProfile userProfile, CancellationToken ct = default);
}
