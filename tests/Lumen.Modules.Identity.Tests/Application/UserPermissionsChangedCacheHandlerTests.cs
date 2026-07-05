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
    public async Task HandleAsync_InvalidatesCacheForUser()
    {
        var userId = Guid.NewGuid();
        var handler = new UserPermissionsChangedCacheHandler(
            _cache,
            NullLogger<UserPermissionsChangedCacheHandler>.Instance);

        await handler.HandleAsync(new UserPermissionsChangedEvent(userId), CancellationToken.None);

        await _cache.Received(1).InvalidateAsync(userId, Arg.Any<CancellationToken>());
    }
}
