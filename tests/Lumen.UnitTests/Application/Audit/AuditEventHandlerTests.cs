using Lumen.Domain.Audit;
using Lumen.Domain.Authorization;
using Lumen.EventHandlers.Audit;
using Lumen.SharedKernel.Constants;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Lumen.UnitTests.Application.Audit;

public sealed class AuditEventHandlerTests
{
    private readonly IAuditRepository _auditRepository = Substitute.For<IAuditRepository>();

    // ──────────────────────────────────────────────────────────────────────
    // UserLoggedIn
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UserLoggedIn_InsertsEntryWithLoginKind()
    {
        var handler = new UserLoggedInAuditHandler(
            _auditRepository,
            NullLogger<UserLoggedInAuditHandler>.Instance);

        await handler.Handle(new UserLoggedIn(Guid.NewGuid(), "alice"), CancellationToken.None);

        await _auditRepository.Received(1).InsertAsync(
            Arg.Is<AuditEntry>(e =>
                e.Kind == AuditEventKinds.AuthLogin &&
                e.Actor == "alice"),
            Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────────────────────────────
    // UserLockedOut
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UserLockedOut_InsertsEntryWithLockoutKind()
    {
        var handler = new UserLockedOutAuditHandler(
            _auditRepository,
            NullLogger<UserLockedOutAuditHandler>.Instance);

        await handler.Handle(new UserLockedOut(Guid.NewGuid(), "bob"), CancellationToken.None);

        await _auditRepository.Received(1).InsertAsync(
            Arg.Is<AuditEntry>(e =>
                e.Kind == AuditEventKinds.AuthLockout &&
                e.Target == "bob"),
            Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────────────────────────────
    // UserPermissionsChanged (cache invalidation)
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UserPermissionsChanged_InsertsEntryWithCacheInvalidateKind()
    {
        var handler = new UserPermissionsChangedAuditHandler(
            _auditRepository,
            NullLogger<UserPermissionsChangedAuditHandler>.Instance);

        var userId = Guid.NewGuid();
        await handler.Handle(new UserPermissionsChanged(userId), CancellationToken.None);

        await _auditRepository.Received(1).InsertAsync(
            Arg.Is<AuditEntry>(e => e.Kind == AuditEventKinds.CacheInvalidate),
            Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────────────────────────────
    // ProfilePermissionsSet
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ProfilePermissionsSet_InsertsEntryWithProfilePermSetKind()
    {
        var handler = new ProfilePermissionsSetAuditHandler(
            _auditRepository,
            NullLogger<ProfilePermissionsSetAuditHandler>.Instance);

        await handler.Handle(
            new ProfilePermissionsSet(Guid.NewGuid(), "Admins", "carol", new List<Guid>()),
            CancellationToken.None);

        await _auditRepository.Received(1).InsertAsync(
            Arg.Is<AuditEntry>(e =>
                e.Kind == AuditEventKinds.ProfilePermSet &&
                e.Actor == "carol" &&
                e.Target == "Admins"),
            Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────────────────────────────
    // UserProfileAssigned
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UserProfileAssigned_InsertsEntryWithAssignKind()
    {
        var handler = new UserProfileAssignedAuditHandler(
            _auditRepository,
            NullLogger<UserProfileAssignedAuditHandler>.Instance);

        await handler.Handle(
            new UserProfileAssigned(Guid.NewGuid(), "dave", Guid.NewGuid(), "Readers"),
            CancellationToken.None);

        await _auditRepository.Received(1).InsertAsync(
            Arg.Is<AuditEntry>(e =>
                e.Kind == AuditEventKinds.UserProfileAssign &&
                e.Target == "dave"),
            Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────────────────────────────
    // UserProfileRemoved
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UserProfileRemoved_InsertsEntryWithRemoveKind()
    {
        var handler = new UserProfileRemovedAuditHandler(
            _auditRepository,
            NullLogger<UserProfileRemovedAuditHandler>.Instance);

        await handler.Handle(
            new UserProfileRemoved(Guid.NewGuid(), "eve", Guid.NewGuid(), "Viewers"),
            CancellationToken.None);

        await _auditRepository.Received(1).InsertAsync(
            Arg.Is<AuditEntry>(e =>
                e.Kind == AuditEventKinds.UserProfileRemove &&
                e.Target == "eve"),
            Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────────────────────────────
    // CleanupJobExecuted
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CleanupJobExecuted_InsertsEntryWithJobCleanupKind()
    {
        var handler = new CleanupJobExecutedAuditHandler(
            _auditRepository,
            NullLogger<CleanupJobExecutedAuditHandler>.Instance);

        await handler.Handle(
            new CleanupJobExecuted("cleanup-expired-refresh-tokens", 7),
            CancellationToken.None);

        await _auditRepository.Received(1).InsertAsync(
            Arg.Is<AuditEntry>(e => e.Kind == AuditEventKinds.JobCleanup),
            Arg.Any<CancellationToken>());
    }
}
