using Lumen.Domain.Authorization;
using FluentAssertions;

namespace Lumen.UnitTests.Domain.Authorization;

public sealed class UserProfileTests
{
    [Fact]
    public void Create_WithValidIds_ReturnsBoundEntity()
    {
        var userId = Guid.NewGuid();
        var profileId = Guid.NewGuid();

        var link = UserProfile.Create(userId, profileId);

        link.UserId.Should().Be(userId);
        link.ProfileId.Should().Be(profileId);
        link.IsDeleted.Should().BeFalse();
        link.DeletedAt.Should().BeNull();
        link.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_WithEmptyUserId_ThrowsArgumentException()
    {
        var act = () => UserProfile.Create(Guid.Empty, Guid.NewGuid());

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithEmptyProfileId_ThrowsArgumentException()
    {
        var act = () => UserProfile.Create(Guid.NewGuid(), Guid.Empty);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SoftDelete_SetsIsDeletedAndDeletedAt()
    {
        var link = UserProfile.Create(Guid.NewGuid(), Guid.NewGuid());
        var before = DateTime.UtcNow;

        link.SoftDelete();

        link.IsDeleted.Should().BeTrue();
        link.DeletedAt.Should().NotBeNull().And.BeOnOrAfter(before);
    }

    [Fact]
    public void Create_TwoLinksForSamePair_HaveDifferentIds()
    {
        var userId = Guid.NewGuid();
        var profileId = Guid.NewGuid();

        var first = UserProfile.Create(userId, profileId);
        var second = UserProfile.Create(userId, profileId);

        first.Id.Should().NotBe(second.Id);
    }
}
