using Lumen.Domain.Audit;
using Lumen.Domain.Authorization;
using Lumen.Domain.Users;
using FluentAssertions;
using DomainProfile = Lumen.Domain.Authorization.Profile;

namespace Lumen.UnitTests.Domain.Users;

public sealed class UserDomainEventsTests
{
    [Fact]
    public void AssignProfile_RaisesPermissionsChangedAndProfileAssigned()
    {
        var user = User.Create("alice@example.com", "alice", "hash");
        var profile = DomainProfile.Create("Editors", "Editors profile");

        var assignment = user.AssignProfile(profile);

        assignment.UserId.Should().Be(user.Id);
        assignment.ProfileId.Should().Be(profile.Id);

        user.DomainEvents.Should().ContainSingle(e => e is UserPermissionsChanged
            && ((UserPermissionsChanged)e).UserId == user.Id);

        user.DomainEvents.Should().ContainSingle(e => e is UserProfileAssigned
            && ((UserProfileAssigned)e).UserId == user.Id
            && ((UserProfileAssigned)e).Username == user.Username
            && ((UserProfileAssigned)e).ProfileId == profile.Id
            && ((UserProfileAssigned)e).ProfileName == profile.Name);
    }

    [Fact]
    public void RemoveProfile_SoftDeletesAssignmentAndRaisesEvents()
    {
        var user = User.Create("alice@example.com", "alice", "hash");
        var profile = DomainProfile.Create("Editors", "Editors profile");
        var assignment = UserProfile.Create(user.Id, profile.Id);

        user.RemoveProfile(assignment, profile);

        assignment.IsDeleted.Should().BeTrue();
        assignment.DeletedAt.Should().NotBeNull();

        user.DomainEvents.Should().ContainSingle(e => e is UserPermissionsChanged
            && ((UserPermissionsChanged)e).UserId == user.Id);

        user.DomainEvents.Should().ContainSingle(e => e is UserProfileRemoved
            && ((UserProfileRemoved)e).UserId == user.Id
            && ((UserProfileRemoved)e).ProfileId == profile.Id
            && ((UserProfileRemoved)e).ProfileName == profile.Name);
    }

    [Fact]
    public void SoftDelete_RaisesPermissionsChanged()
    {
        var user = User.Create("alice@example.com", "alice", "hash");

        user.SoftDelete();

        user.DomainEvents.Should().ContainSingle(e => e is UserPermissionsChanged
            && ((UserPermissionsChanged)e).UserId == user.Id);
    }

    [Fact]
    public void RecordLogin_RaisesUserLoggedIn()
    {
        var user = User.Create("alice@example.com", "alice", "hash");

        user.RecordLogin();

        user.DomainEvents.Should().ContainSingle(e => e is UserLoggedIn
            && ((UserLoggedIn)e).UserId == user.Id
            && ((UserLoggedIn)e).Username == user.Username);
    }

    [Fact]
    public void RecordFailedLogin_BelowThreshold_RaisesNoEvent()
    {
        var user = User.Create("alice@example.com", "alice", "hash");

        user.RecordFailedLogin(lockoutThreshold: 5, lockoutDuration: TimeSpan.FromMinutes(15));

        user.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void RecordFailedLogin_WhenThresholdReached_RaisesUserLockedOut()
    {
        var user = User.Create("alice@example.com", "alice", "hash");

        user.RecordFailedLogin(lockoutThreshold: 1, lockoutDuration: TimeSpan.FromMinutes(15));

        user.DomainEvents.Should().ContainSingle(e => e is UserLockedOut
            && ((UserLockedOut)e).UserId == user.Id
            && ((UserLockedOut)e).Username == user.Username);
    }

    [Fact]
    public void ClearDomainEvents_RemovesAllRaisedEvents()
    {
        var user = User.Create("alice@example.com", "alice", "hash");
        user.SoftDelete();

        user.ClearDomainEvents();

        user.DomainEvents.Should().BeEmpty();
    }
}
