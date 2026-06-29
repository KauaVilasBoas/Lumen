using Lumen.Modules.Audit.Domain;
using Lumen.SharedKernel.Constants;
using FluentAssertions;

namespace Lumen.Modules.Audit.Tests.EventHandlers;

public sealed class AuditEntryDomainTests
{
    [Theory]
    [InlineData(AuditEventKinds.AuthLogin, "alice", null, "User 'alice' logged in.")]
    [InlineData(AuditEventKinds.AuthLockout, null, "bob", "Account 'bob' locked out after repeated failed login attempts.")]
    [InlineData(AuditEventKinds.JobCleanup, null, null, "Job 'cleanup' executed — 3 record(s) deleted.")]
    public void AuditEntry_Create_SetsAllFieldsCorrectly(
        string kind, string? actor, string? target, string message)
    {
        var entry = AuditEntry.Create(kind, actor, target, message);

        entry.Kind.Should().Be(kind);
        entry.Actor.Should().Be(actor);
        entry.Target.Should().Be(target);
        entry.Message.Should().Be(message);
        entry.Id.Should().NotBeEmpty();
        entry.OccurredAt.Should().BeCloseTo(DateTime.UtcNow, precision: TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void AuditEntry_Create_ThrowsWhenKindIsEmpty()
    {
        var act = () => AuditEntry.Create(string.Empty, "actor", "target", "message");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AuditEntry_Create_ThrowsWhenMessageIsEmpty()
    {
        var act = () => AuditEntry.Create(AuditEventKinds.AuthLogin, "actor", "target", string.Empty);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AuditEntry_Create_AssignsUniqueIdPerInstance()
    {
        var entry1 = AuditEntry.Create(AuditEventKinds.AuthLogin, "a", null, "msg");
        var entry2 = AuditEntry.Create(AuditEventKinds.AuthLogin, "a", null, "msg");

        entry1.Id.Should().NotBe(entry2.Id);
    }
}
