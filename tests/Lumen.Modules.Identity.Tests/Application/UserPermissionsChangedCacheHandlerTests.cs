using FluentAssertions;
using Lumen.Authorization.Application.EventHandlers;
using Lumen.Authorization.Contracts.Events;
using Lumen.Authorization.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Lumen.Modules.Identity.Tests.Application;

public sealed class UserPermissionsChangedCacheHandlerTests
{
    private readonly IUserPermissionCache _cache = Substitute.For<IUserPermissionCache>();

    [Fact]
    public async Task HandleAsync_GlobalEvent_InvalidatesCacheWithNullScope()
    {
        var userId = Guid.NewGuid();
        var handler = new UserPermissionsChangedCacheHandler(
            _cache,
            NullLogger<UserPermissionsChangedCacheHandler>.Instance);

        await handler.HandleAsync(new UserPermissionsChangedEvent(userId), CancellationToken.None);

        // ScopeId defaults to null — invalidates the global permission cache entry.
        await _cache.Received(1).InvalidateAsync(userId, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ScopedEvent_InvalidatesCacheForSpecificScope()
    {
        var userId = Guid.NewGuid();
        var scopeId = Guid.NewGuid();
        var handler = new UserPermissionsChangedCacheHandler(
            _cache,
            NullLogger<UserPermissionsChangedCacheHandler>.Instance);

        await handler.HandleAsync(new UserPermissionsChangedEvent(userId, scopeId), CancellationToken.None);

        // Only the targeted (userId, scopeId) cache entry is invalidated, not global.
        await _cache.Received(1).InvalidateAsync(userId, scopeId, Arg.Any<CancellationToken>());
        await _cache.DidNotReceive().InvalidateAsync(userId, null, Arg.Any<CancellationToken>());
    }
}
