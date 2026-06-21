using AegisIdentity.Domain.Authorization;
using Microsoft.Extensions.Logging;

namespace AegisIdentity.DataAccess.Cache;

internal sealed class UserPermissionService : IUserPermissionService
{
    private readonly IUserPermissionCache _cache;
    private readonly IProfileRepository _profileRepository;
    private readonly ILogger<UserPermissionService> _logger;

    public UserPermissionService(
        IUserPermissionCache cache,
        IProfileRepository profileRepository,
        ILogger<UserPermissionService> logger)
    {
        _cache = cache;
        _profileRepository = profileRepository;
        _logger = logger;
    }

    public async Task<HashSet<string>> GetPermissionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var cached = await _cache.GetAsync(userId, cancellationToken);

        if (cached is not null)
            return cached;

        _logger.LogDebug("Permission cache miss for user {UserId}. Falling back to database.", userId);

        var fromDb = await _profileRepository.GetPermissionCodesByUserIdAsync(userId, cancellationToken);

        await _cache.SetAsync(userId, fromDb, cancellationToken);

        return fromDb;
    }

    public async Task<bool> HasPermissionAsync(Guid userId, string permissionCode, CancellationToken cancellationToken = default)
    {
        var permissions = await GetPermissionsAsync(userId, cancellationToken);
        return permissions.Contains(permissionCode);
    }
}
