using Lumen.Domain.Audit;
using Lumen.Domain.Authorization;
using Lumen.EventHandlers.Authorization;
using MediatR;
using NSubstitute;

namespace Lumen.UnitTests.Application.Authorization;

public sealed class ProfileCacheInvalidationHandlerTests
{
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();

    [Fact]
    public async Task ProfileDeleted_PublishesPermissionsChangedForEachAffectedUser()
    {
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var handler = new ProfileDeletedCacheInvalidationHandler(_publisher);

        await handler.Handle(
            new ProfileDeleted(Guid.NewGuid(), new List<Guid> { userId1, userId2 }),
            CancellationToken.None);

        await _publisher.Received(1).Publish(
            Arg.Is<UserPermissionsChanged>(e => e.UserId == userId1), Arg.Any<CancellationToken>());
        await _publisher.Received(1).Publish(
            Arg.Is<UserPermissionsChanged>(e => e.UserId == userId2), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProfileDeleted_WithNoAffectedUsers_PublishesNothing()
    {
        var handler = new ProfileDeletedCacheInvalidationHandler(_publisher);

        await handler.Handle(
            new ProfileDeleted(Guid.NewGuid(), new List<Guid>()),
            CancellationToken.None);

        await _publisher.DidNotReceive().Publish(
            Arg.Any<UserPermissionsChanged>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProfilePermissionsSet_PublishesPermissionsChangedForEachAffectedUser()
    {
        var userId = Guid.NewGuid();
        var handler = new ProfilePermissionsSetCacheInvalidationHandler(_publisher);

        await handler.Handle(
            new ProfilePermissionsSet(Guid.NewGuid(), "Editors", "carol", new List<Guid> { userId }),
            CancellationToken.None);

        await _publisher.Received(1).Publish(
            Arg.Is<UserPermissionsChanged>(e => e.UserId == userId), Arg.Any<CancellationToken>());
    }
}
