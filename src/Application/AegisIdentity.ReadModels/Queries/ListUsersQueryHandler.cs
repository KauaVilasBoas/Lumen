using AegisIdentity.Domain.Authorization;
using AegisIdentity.Domain.Users;
using MediatR;

namespace AegisIdentity.ReadModels.Queries;

public sealed class ListUsersQueryHandler
    : IRequestHandler<ListUsersQueryHandler.Query, ListUsersQueryHandler.PagedResult>
{
    /// <summary>
    /// Represents the requested state filter for the users listing.
    /// </summary>
    public enum UserStateFilter
    {
        Active,
        Locked,
        Pending,
        Deleted,
        All,
    }

    public sealed record Query(
        string? Search,
        UserStateFilter State,
        int Page,
        int PageSize) : IRequest<PagedResult>;

    public sealed record UserResult(
        Guid Id,
        string Username,
        string Email,
        string State,
        bool IsBootstrap,
        DateTime CreatedAt,
        DateTime? LastLoginAt,
        DateTime? EmailConfirmedAt,
        DateTime? LockoutEndAt,
        int ProfileCount,
        int ResolvedPermissionCount);

    public sealed record PagedResult(
        IReadOnlyList<UserResult> Items,
        int Page,
        int PageSize,
        int Total);

    private readonly IUserRepository _userRepository;
    private readonly IProfileRepository _profileRepository;

    public ListUsersQueryHandler(
        IUserRepository userRepository,
        IProfileRepository profileRepository)
    {
        _userRepository = userRepository;
        _profileRepository = profileRepository;
    }

    public async Task<PagedResult> Handle(Query query, CancellationToken ct)
    {
        // Deleted users are hidden by the global query filter; bypass it only
        // when the caller explicitly requests deleted or all users.
        var includeDeleted = query.State is UserStateFilter.Deleted or UserStateFilter.All;

        var (users, total) = await _userRepository.ListAsync(
            query.Search,
            includeDeleted,
            query.Page,
            query.PageSize,
            ct);

        // Apply in-memory state filter after the query.
        // The database returns either all rows (includeDeleted) or only non-deleted rows.
        // We still need to narrow down to the specific state when the caller asks for
        // locked, pending or active users.
        var now = DateTime.UtcNow;
        var filtered = query.State switch
        {
            UserStateFilter.Active  => users.Where(u => DeriveState(u, now) == "active").ToList(),
            UserStateFilter.Locked  => users.Where(u => DeriveState(u, now) == "locked").ToList(),
            UserStateFilter.Pending => users.Where(u => DeriveState(u, now) == "pending").ToList(),
            UserStateFilter.Deleted => users.Where(u => DeriveState(u, now) == "deleted").ToList(),
            _                       => (IReadOnlyList<User>)users,
        };

        if (filtered.Count == 0)
            return new PagedResult([], query.Page, query.PageSize, 0);

        // Resolve profile counts and resolved permission counts in batch to avoid N+1.
        var userIds = filtered.Select(u => u.Id).ToList();
        var profilesByUser = await BatchProfilesByUserAsync(userIds, ct);
        var permissionCountByUser = await BatchPermissionCountByUserAsync(profilesByUser, ct);

        var items = filtered
            .Select(u => new UserResult(
                Id: u.Id,
                Username: u.Username,
                Email: u.Email,
                State: DeriveState(u, now),
                IsBootstrap: IsBootstrapUser(u),
                CreatedAt: u.CreatedAt,
                LastLoginAt: u.LastLoginAt,
                EmailConfirmedAt: u.EmailConfirmedAt,
                LockoutEndAt: u.LockedUntil,
                ProfileCount: profilesByUser.TryGetValue(u.Id, out var profiles) ? profiles.Count : 0,
                ResolvedPermissionCount: permissionCountByUser.GetValueOrDefault(u.Id, 0)))
            .ToList();

        return new PagedResult(items, query.Page, query.PageSize, total);
    }

    /// <summary>
    /// Derives the logical state of a user from its domain fields.
    /// Precedence: deleted > locked > pending > active.
    /// </summary>
    private static string DeriveState(User user, DateTime now)
    {
        if (user.IsDeleted)
            return "deleted";

        if (user.LockedUntil.HasValue && user.LockedUntil.Value > now)
            return "locked";

        if (user.EmailConfirmedAt is null)
            return "pending";

        return "active";
    }

    /// <summary>
    /// A bootstrap user is the first user ever created (lowest CreatedAt).
    /// In practice this is the user seeded automatically on first startup.
    /// We identify it by checking whether it has no EmailConfirmedAt set
    /// and its account was created before any other — instead we use the
    /// simpler domain convention: the repository does not expose an IsBootstrap
    /// flag yet, so we derive it conservatively as false until that column exists.
    /// </summary>
    private static bool IsBootstrapUser(User user)
    {
        // IsBootstrap is not yet a domain field. Returning false is safe and honest.
        // When the domain exposes this property this method should delegate to it.
        return false;
    }

    /// <summary>
    /// Loads the profiles for all <paramref name="userIds"/> in a single batched
    /// query per user (using the existing repository method) and groups them by userId.
    /// </summary>
    private async Task<Dictionary<Guid, IReadOnlyList<Profile>>> BatchProfilesByUserAsync(
        IReadOnlyList<Guid> userIds,
        CancellationToken ct)
    {
        var result = new Dictionary<Guid, IReadOnlyList<Profile>>(userIds.Count);

        // GetProfilesByUserIdAsync already filters at the database level per user.
        // We call it once per user in the current page — typically ≤ 20 calls for
        // the default page size. A future optimisation could add a batch overload to
        // IProfileRepository, but at this scale the per-user approach is acceptable.
        var tasks = userIds
            .Select(async userId =>
            {
                var profiles = await _profileRepository.GetProfilesByUserIdAsync(userId, ct);
                return (userId, profiles);
            });

        foreach (var (userId, profiles) in await Task.WhenAll(tasks))
            result[userId] = profiles;

        return result;
    }

    /// <summary>
    /// Calculates the number of resolved (distinct) permissions for each user
    /// from their already-loaded profiles, using the permission codes available
    /// via <see cref="IProfileRepository.GetPermissionCodesByUserIdAsync"/>.
    /// </summary>
    private async Task<Dictionary<Guid, int>> BatchPermissionCountByUserAsync(
        Dictionary<Guid, IReadOnlyList<Profile>> profilesByUser,
        CancellationToken ct)
    {
        var result = new Dictionary<Guid, int>(profilesByUser.Count);

        var tasks = profilesByUser
            .Where(kv => kv.Value.Count > 0)
            .Select(async kv =>
            {
                var codes = await _profileRepository.GetPermissionCodesByUserIdAsync(kv.Key, ct);
                return (userId: kv.Key, count: codes.Count);
            });

        foreach (var (userId, count) in await Task.WhenAll(tasks))
            result[userId] = count;

        // Users with no profiles get 0 permissions (already the default).
        foreach (var userId in profilesByUser.Where(kv => kv.Value.Count == 0).Select(kv => kv.Key))
            result[userId] = 0;

        return result;
    }
}
