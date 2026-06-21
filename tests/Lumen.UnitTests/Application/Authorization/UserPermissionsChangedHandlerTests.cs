using Lumen.Domain.Authorization;
using Lumen.EventHandlers.Authorization;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Lumen.UnitTests.Application.Authorization;

public sealed class UserPermissionsChangedHandlerTests
{
    private readonly IUserPermissionCache _cache;
    private readonly UserPermissionsChangedHandler _sut;

    public UserPermissionsChangedHandlerTests()
    {
        _cache = Substitute.For<IUserPermissionCache>();
        _sut = new UserPermissionsChangedHandler(
            _cache,
            NullLogger<UserPermissionsChangedHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WhenCacheInvalidationSucceeds_CompletesNormally()
    {
        var userId = Guid.NewGuid();
        var notification = new UserPermissionsChanged(userId);

        _cache.InvalidateAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var act = async () => await _sut.Handle(notification, CancellationToken.None);

        await act.Should().NotThrowAsync();
        await _cache.Received(1).InvalidateAsync(userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCacheInvalidationFails_PropagatesException()
    {
        // Fail-closed: the handler must not swallow the cache exception so that
        // the originating command knows the revocation did not fully complete.
        var userId = Guid.NewGuid();
        var notification = new UserPermissionsChanged(userId);

        _cache.InvalidateAsync(userId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Redis unavailable."));

        var act = async () => await _sut.Handle(notification, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Redis unavailable.");
    }
}
