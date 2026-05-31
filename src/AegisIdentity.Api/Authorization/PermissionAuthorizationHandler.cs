using System.Security.Claims;
using AegisIdentity.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace AegisIdentity.Api.Authorization;

public sealed class PermissionAuthorizationHandler
    : AuthorizationHandler<PermissionRequirement>
{
    private readonly IUserPermissionCache _cache;
    private readonly IProfileRepository _profileRepository;
    private readonly ILogger<PermissionAuthorizationHandler> _logger;

    public PermissionAuthorizationHandler(
        IUserPermissionCache cache,
        IProfileRepository profileRepository,
        ILogger<PermissionAuthorizationHandler> logger)
    {
        _cache = cache;
        _profileRepository = profileRepository;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var sub = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(sub, out var userId))
            return;

        var permissions = await ResolvePermissionsAsync(userId);

        if (permissions.Contains(requirement.Code))
            context.Succeed(requirement);
    }

    private async Task<HashSet<string>> ResolvePermissionsAsync(Guid userId)
    {
        var cached = await _cache.GetAsync(userId);

        if (cached is not null)
            return cached;

        _logger.LogDebug("Permission cache miss for user {UserId}. Falling back to database.", userId);

        var fromDb = await _profileRepository.GetPermissionCodesByUserIdAsync(userId);

        await _cache.SetAsync(userId, fromDb);

        return fromDb;
    }
}
