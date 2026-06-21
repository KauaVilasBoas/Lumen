using FluentAssertions;
using DomainProfile = AegisIdentity.Domain.Authorization.Profile;

namespace AegisIdentity.UnitTests.Domain.Authorization;

public sealed class ProfileTests
{
    [Fact]
    public void Create_WithValidArgs_ReturnsProfile()
    {
        var profile = DomainProfile.Create("Admin", "Full access profile");

        profile.Name.Should().Be("Admin");
        profile.Description.Should().Be("Full access profile");
        profile.IsSystem.Should().BeFalse();
        profile.IsDeleted.Should().BeFalse();
        profile.DeletedAt.Should().BeNull();
        profile.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_AsSystemProfile_SetsIsSystemTrue()
    {
        var profile = DomainProfile.Create("SuperAdmin", "System-managed profile", isSystem: true);

        profile.IsSystem.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "Description")]
    [InlineData("   ", "Description")]
    [InlineData("Name", "")]
    [InlineData("Name", "   ")]
    public void Create_WithBlankRequiredField_ThrowsArgumentException(string name, string description)
    {
        var act = () => DomainProfile.Create(name, description);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SoftDelete_OnRegularProfile_SetsIsDeletedAndDeletedAt()
    {
        var profile = DomainProfile.Create("Viewer", "Read-only access");
        var before = DateTime.UtcNow;

        profile.SoftDelete();

        profile.IsDeleted.Should().BeTrue();
        profile.DeletedAt.Should().NotBeNull().And.BeOnOrAfter(before);
    }

    [Fact]
    public void SoftDelete_OnSystemProfile_ThrowsInvalidOperationException()
    {
        var profile = DomainProfile.Create("SuperAdmin", "System profile", isSystem: true);

        var act = () => profile.SoftDelete();

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*SuperAdmin*");
    }

    [Fact]
    public void SoftDelete_OnSystemProfile_DoesNotMutateState()
    {
        var profile = DomainProfile.Create("SuperAdmin", "System profile", isSystem: true);

        try { profile.SoftDelete(); } catch (InvalidOperationException) { }

        profile.IsDeleted.Should().BeFalse();
        profile.DeletedAt.Should().BeNull();
    }
}
