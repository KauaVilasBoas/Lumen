using AegisIdentity.Domain.Audit;
using AegisIdentity.ReadModels.Queries;
using AegisIdentity.SharedKernel.Constants;
using FluentAssertions;
using NSubstitute;

namespace AegisIdentity.UnitTests.Application.ReadModels;

public sealed class GetRecentAuditFeedQueryHandlerTests
{
    private readonly IAuditRepository _auditRepository = Substitute.For<IAuditRepository>();

    // ──────────────────────────────────────────────────────────────────────
    // Empty result
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_NoEntries_ReturnsEmptyList()
    {
        _auditRepository.GetRecentAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<AuditEntry>());

        var result = await InvokeHandler(take: 10);

        result.Should().BeEmpty();
    }

    // ──────────────────────────────────────────────────────────────────────
    // Result mapping
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithEntries_MapsAllFieldsCorrectly()
    {
        var occurredAt = new DateTime(2026, 6, 8, 12, 0, 0, DateTimeKind.Utc);
        var entry = AuditEntry.Create(
            kind: AuditEventKinds.AuthLogin,
            actor: "alice",
            target: null,
            message: "User 'alice' logged in.");

        _auditRepository.GetRecentAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new[] { entry });

        var result = await InvokeHandler(take: 5);

        result.Should().HaveCount(1);
        var item = result[0];
        item.Kind.Should().Be(AuditEventKinds.AuthLogin);
        item.Actor.Should().Be("alice");
        item.Target.Should().BeNull();
        item.Message.Should().Be("User 'alice' logged in.");
    }

    [Fact]
    public async Task Handle_ValidTake_PassesTakeToRepository()
    {
        _auditRepository.GetRecentAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<AuditEntry>());

        await InvokeHandler(take: 20);

        await _auditRepository.Received(1).GetRecentAsync(
            Arg.Is<int>(n => n == 20),
            Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────────────────────────────
    // Multiple entries maintain repository order
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_MultipleEntries_PreservesRepositoryOrder()
    {
        var first  = AuditEntry.Create(AuditEventKinds.AuthLogin, "alice", null, "first");
        var second = AuditEntry.Create(AuditEventKinds.AuthLockout, null, "bob", "second");

        _auditRepository.GetRecentAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new[] { first, second });

        var result = await InvokeHandler(take: 10);

        result.Should().HaveCount(2);
        result[0].Kind.Should().Be(AuditEventKinds.AuthLogin);
        result[1].Kind.Should().Be(AuditEventKinds.AuthLockout);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────

    private Task<IReadOnlyList<GetRecentAuditFeedQueryHandler.AuditEntryResult>> InvokeHandler(int take)
    {
        var handler = new GetRecentAuditFeedQueryHandler(_auditRepository);
        return handler.Handle(new GetRecentAuditFeedQueryHandler.Query(take), CancellationToken.None);
    }
}
