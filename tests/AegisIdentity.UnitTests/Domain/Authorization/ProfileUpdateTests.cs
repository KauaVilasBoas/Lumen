using FluentAssertions;
using DomainProfile = AegisIdentity.Domain.Authorization.Profile;

namespace AegisIdentity.UnitTests.Domain.Authorization;

public sealed class ProfileUpdateTests
{
    [Fact]
    public void Update_WithValidArgs_ChangesNameAndDescription()
    {
        var profile = DomainProfile.Create("OldName", "Old description");

        profile.Update("NewName", "New description");

        profile.Name.Should().Be("NewName");
        profile.Description.Should().Be("New description");
    }

    [Theory]
    [InlineData("", "Description")]
    [InlineData("   ", "Description")]
    [InlineData("Name", "")]
    [InlineData("Name", "   ")]
    public void Update_WithBlankRequiredField_ThrowsArgumentException(string name, string description)
    {
        var profile = DomainProfile.Create("ValidName", "Valid description");

        var act = () => profile.Update(name, description);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Update_DoesNotMutateIsSystemOrIsDeleted()
    {
        var profile = DomainProfile.Create("Name", "Description", isSystem: true);

        profile.Update("NewName", "New description");

        profile.IsSystem.Should().BeTrue();
        profile.IsDeleted.Should().BeFalse();
    }
}
