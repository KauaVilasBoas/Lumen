using AegisIdentity.Domain.Authorization;
using FluentAssertions;

namespace AegisIdentity.UnitTests.Domain.Authorization;

public sealed class PermissionProfileTests
{
    [Fact]
    public void Create_WithValidIds_ReturnsBoundEntity()
    {
        var permissionId = Guid.NewGuid();
        var profileId = Guid.NewGuid();

        var link = PermissionProfile.Create(permissionId, profileId);

        link.PermissionId.Should().Be(permissionId);
        link.ProfileId.Should().Be(profileId);
        link.IsDeleted.Should().BeFalse();
        link.DeletedAt.Should().BeNull();
        link.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_WithEmptyPermissionId_ThrowsArgumentException()
    {
        var act = () => PermissionProfile.Create(Guid.Empty, Guid.NewGuid());

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithEmptyProfileId_ThrowsArgumentException()
    {
        var act = () => PermissionProfile.Create(Guid.NewGuid(), Guid.Empty);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SoftDelete_SetsIsDeletedAndDeletedAt()
    {
        var link = PermissionProfile.Create(Guid.NewGuid(), Guid.NewGuid());
        var before = DateTime.UtcNow;

        link.SoftDelete();

        link.IsDeleted.Should().BeTrue();
        link.DeletedAt.Should().NotBeNull().And.BeOnOrAfter(before);
    }

    [Fact]
    public void Create_TwoLinksForSamePair_HaveDifferentIds()
    {
        var permissionId = Guid.NewGuid();
        var profileId = Guid.NewGuid();

        var first = PermissionProfile.Create(permissionId, profileId);
        var second = PermissionProfile.Create(permissionId, profileId);

        first.Id.Should().NotBe(second.Id);
    }
}
