using Lumen.Domain.Audit;
using Lumen.Domain.Authorization;
using Lumen.EventHandlers.Audit;
using Lumen.Modularity;
using Lumen.Modules.Audit.Contracts.Events;
using FluentAssertions;
using NSubstitute;

namespace Lumen.UnitTests.Application.Audit;

public sealed class AuditEventHandlerTests
{
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();

    // ──────────────────────────────────────────────────────────────────────
    // UserLoggedIn — bridge publishes UserLoggedInEvent
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UserLoggedIn_PublishesUserLoggedInEventOnBus()
    {
        var handler = new UserLoggedInAuditHandler(_eventBus);
        var userId  = Guid.NewGuid();

        await handler.Handle(new UserLoggedIn(userId, "alice"), CancellationToken.None);

        await _eventBus.Received(1).PublishAsync(
            Arg.Is<UserLoggedInEvent>(e => e.UserId == userId && e.Username == "alice"),
            Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────────────────────────────
    // UserLockedOut — bridge publishes UserLockedOutEvent
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UserLockedOut_PublishesUserLockedOutEventOnBus()
    {
        var handler = new UserLockedOutAuditHandler(_eventBus);
        var userId  = Guid.NewGuid();

        await handler.Handle(new UserLockedOut(userId, "bob"), CancellationToken.None);

        await _eventBus.Received(1).PublishAsync(
            Arg.Is<UserLockedOutEvent>(e => e.UserId == userId && e.Username == "bob"),
            Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────────────────────────────
    // UserPermissionsChanged — bridge publishes UserPermissionsChangedEvent
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UserPermissionsChanged_PublishesUserPermissionsChangedEventOnBus()
    {
        var handler = new UserPermissionsChangedAuditHandler(_eventBus);
        var userId  = Guid.NewGuid();

        await handler.Handle(new UserPermissionsChanged(userId), CancellationToken.None);

        await _eventBus.Received(1).PublishAsync(
            Arg.Is<UserPermissionsChangedEvent>(e => e.UserId == userId),
            Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────────────────────────────
    // ProfilePermissionsSet — bridge publishes ProfilePermissionsSetEvent
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ProfilePermissionsSet_PublishesProfilePermissionsSetEventOnBus()
    {
        var handler   = new ProfilePermissionsSetAuditHandler(_eventBus);
        var profileId = Guid.NewGuid();

        await handler.Handle(
            new ProfilePermissionsSet(profileId, "Admins", "carol"),
            CancellationToken.None);

        await _eventBus.Received(1).PublishAsync(
            Arg.Is<ProfilePermissionsSetEvent>(e =>
                e.ProfileId == profileId &&
                e.ProfileName == "Admins" &&
                e.ActorUsername == "carol"),
            Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────────────────────────────
    // UserProfileAssigned — bridge publishes UserProfileAssignedEvent
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UserProfileAssigned_PublishesUserProfileAssignedEventOnBus()
    {
        var handler   = new UserProfileAssignedAuditHandler(_eventBus);
        var userId    = Guid.NewGuid();
        var profileId = Guid.NewGuid();

        await handler.Handle(
            new UserProfileAssigned(userId, "dave", profileId, "Readers"),
            CancellationToken.None);

        await _eventBus.Received(1).PublishAsync(
            Arg.Is<UserProfileAssignedEvent>(e =>
                e.UserId == userId &&
                e.Username == "dave" &&
                e.ProfileId == profileId &&
                e.ProfileName == "Readers"),
            Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────────────────────────────
    // UserProfileRemoved — bridge publishes UserProfileRemovedEvent
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UserProfileRemoved_PublishesUserProfileRemovedEventOnBus()
    {
        var handler   = new UserProfileRemovedAuditHandler(_eventBus);
        var userId    = Guid.NewGuid();
        var profileId = Guid.NewGuid();

        await handler.Handle(
            new UserProfileRemoved(userId, "eve", profileId, "Viewers"),
            CancellationToken.None);

        await _eventBus.Received(1).PublishAsync(
            Arg.Is<UserProfileRemovedEvent>(e =>
                e.UserId == userId &&
                e.Username == "eve" &&
                e.ProfileId == profileId &&
                e.ProfileName == "Viewers"),
            Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────────────────────────────
    // CleanupJobExecuted — bridge publishes CleanupJobExecutedEvent
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CleanupJobExecuted_PublishesCleanupJobExecutedEventOnBus()
    {
        var handler = new CleanupJobExecutedAuditHandler(_eventBus);

        await handler.Handle(
            new CleanupJobExecuted("cleanup-expired-refresh-tokens", 7),
            CancellationToken.None);

        await _eventBus.Received(1).PublishAsync(
            Arg.Is<CleanupJobExecutedEvent>(e =>
                e.JobName == "cleanup-expired-refresh-tokens" &&
                e.DeletedCount == 7),
            Arg.Any<CancellationToken>());
    }
}
