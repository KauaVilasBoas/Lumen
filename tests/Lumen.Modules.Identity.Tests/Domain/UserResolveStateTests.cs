using FluentAssertions;
using Lumen.Modules.Identity.Domain.Users;
using Lumen.SharedKernel.Constants;

namespace Lumen.Modules.Identity.Tests.Domain;

public sealed class UserResolveStateTests
{
    private static readonly DateTime ReferenceTime = new(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ResolveState_EmailNotConfirmed_ReturnsPending()
    {
        var user = User.Create("alice@test.com", "alice", "hash");

        user.ResolveState(ReferenceTime).Should().Be(UserStates.Pending);
    }

    [Fact]
    public void ResolveState_EmailConfirmedAndNotLocked_ReturnsActive()
    {
        var user = User.Create("alice@test.com", "alice", "hash");
        user.ConfirmEmail();

        user.ResolveState(ReferenceTime).Should().Be(UserStates.Active);
    }

    [Fact]
    public void ResolveState_EmailConfirmedAndLockoutStillActive_ReturnsLocked()
    {
        var user = User.Create("alice@test.com", "alice", "hash");
        user.ConfirmEmail();
        user.RecordFailedLogin(lockoutThreshold: 1, lockoutDuration: TimeSpan.FromHours(1));

        var asOf = DateTime.UtcNow.AddMinutes(30);

        user.ResolveState(asOf).Should().Be(UserStates.Locked);
    }

    [Fact]
    public void ResolveState_EmailConfirmedAndLockoutExpired_ReturnsActive()
    {
        var user = User.Create("alice@test.com", "alice", "hash");
        user.ConfirmEmail();
        user.RecordFailedLogin(lockoutThreshold: 1, lockoutDuration: TimeSpan.FromHours(1));

        var asOf = DateTime.UtcNow.AddHours(2);

        user.ResolveState(asOf).Should().Be(UserStates.Active);
    }

    [Fact]
    public void ResolveState_SoftDeleted_ReturnsDeleted()
    {
        var user = User.Create("alice@test.com", "alice", "hash");
        user.ConfirmEmail();
        user.SoftDelete();

        user.ResolveState(ReferenceTime).Should().Be(UserStates.Deleted);
    }

    [Fact]
    public void ResolveState_DeletedTakesPrecedenceOverLocked()
    {
        var user = User.Create("alice@test.com", "alice", "hash");
        user.ConfirmEmail();
        user.RecordFailedLogin(lockoutThreshold: 1, lockoutDuration: TimeSpan.FromHours(1));
        user.SoftDelete();

        var asOf = DateTime.UtcNow.AddMinutes(30);

        user.ResolveState(asOf).Should().Be(UserStates.Deleted);
    }

    [Fact]
    public void ResolveState_LockedTakesPrecedenceOverPending()
    {
        var user = User.Create("alice@test.com", "alice", "hash");
        user.RecordFailedLogin(lockoutThreshold: 1, lockoutDuration: TimeSpan.FromHours(1));

        var asOf = DateTime.UtcNow.AddMinutes(30);

        user.ResolveState(asOf).Should().Be(UserStates.Locked);
    }

    [Fact]
    public void ResolveState_DeletedTakesPrecedenceOverPending()
    {
        var user = User.Create("alice@test.com", "alice", "hash");
        user.SoftDelete();

        user.ResolveState(ReferenceTime).Should().Be(UserStates.Deleted);
    }

    [Fact]
    public void ResolveState_LockoutBoundary_ExactExpiryIsNotLocked()
    {
        var user = User.Create("alice@test.com", "alice", "hash");
        user.ConfirmEmail();
        user.RecordFailedLogin(lockoutThreshold: 1, lockoutDuration: TimeSpan.FromHours(1));

        var exactExpiry = DateTime.UtcNow.AddHours(1);

        user.ResolveState(exactExpiry).Should().Be(UserStates.Active);
    }
}
