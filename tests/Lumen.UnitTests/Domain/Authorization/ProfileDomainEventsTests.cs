using Lumen.Domain.Audit;
using Lumen.Domain.Authorization;
using Lumen.SharedKernel.Exceptions;
using FluentAssertions;
using DomainProfile = Lumen.Domain.Authorization.Profile;

namespace Lumen.UnitTests.Domain.Authorization;

public sealed class ProfileDomainEventsTests
{
    [Fact]
    public void Delete_SoftDeletesAndRaisesProfileDeleted()
    {
        var profile = DomainProfile.Create("Editors", "Editors profile");
        var affectedUserIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

        profile.Delete(affectedUserIds);

        profile.IsDeleted.Should().BeTrue();
        profile.DeletedAt.Should().NotBeNull();

        profile.DomainEvents.Should().ContainSingle(e => e is ProfileDeleted
            && ((ProfileDeleted)e).ProfileId == profile.Id
            && ((ProfileDeleted)e).AffectedUserIds.SequenceEqual(affectedUserIds));
    }

    [Fact]
    public void Delete_WhenSystemProfile_ThrowsAndRaisesNoEvent()
    {
        var systemProfile = DomainProfile.Create("Administrator", "System profile", isSystem: true);

        var act = () => systemProfile.Delete(new List<Guid>());

        act.Should().Throw<ForbiddenException>();
        systemProfile.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void RecordPermissionsSet_RaisesProfilePermissionsSet()
    {
        var profile = DomainProfile.Create("Editors", "Editors profile");
        var affectedUserIds = new List<Guid> { Guid.NewGuid() };

        profile.RecordPermissionsSet("carol", affectedUserIds);

        profile.DomainEvents.Should().ContainSingle(e => e is ProfilePermissionsSet
            && ((ProfilePermissionsSet)e).ProfileId == profile.Id
            && ((ProfilePermissionsSet)e).ProfileName == profile.Name
            && ((ProfilePermissionsSet)e).ActorUsername == "carol"
            && ((ProfilePermissionsSet)e).AffectedUserIds.SequenceEqual(affectedUserIds));
    }
}
