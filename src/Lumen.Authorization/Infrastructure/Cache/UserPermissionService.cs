using Lumen.Authorization.Contracts;
using Lumen.Authorization.Domain;
using Microsoft.Extensions.Logging;

namespace Lumen.Authorization.Infrastructure.Cache;

internal sealed class UserPermissionService : Domain.IUserPermissionService, Contracts.IUserPermissionService
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

    public async Task<HashSet<string>> GetPermissionsAsync(
        Guid userId,
        Guid? scopeId = null,
        CancellationToken cancellationToken = default)
    {
        var cached = await _cache.GetAsync(userId, scopeId, cancellationToken);

        if (cached is not null)
            return cached;

        _logger.LogDebug(
            "Permission cache miss for user {UserId} scope {ScopeId}. Falling back to database.",
            userId,
            scopeId);

        var fromDb = await _profileRepository.GetPermissionCodesByUserIdAsync(userId, scopeId, cancellationToken);

        await _cache.SetAsync(userId, scopeId, fromDb, cancellationToken);

        return fromDb;
    }

    public async Task<bool> HasPermissionAsync(
        Guid userId,
        string permissionCode,
        Guid? scopeId = null,
        CancellationToken cancellationToken = default)
    {
        var permissions = await GetPermissionsAsync(userId, scopeId, cancellationToken);
        return permissions.Contains(permissionCode);
    }
}
