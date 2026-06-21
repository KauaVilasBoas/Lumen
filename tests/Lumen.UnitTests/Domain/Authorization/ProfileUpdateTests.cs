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

    [Fact]
    public void Update_SystemProfileWithSameName_DoesNotThrow()
    {
        // The domain entity has no rename guard — protection lives in the application handler.
        // This test documents that intent: domain Update() is always callable; handlers decide policy.
        var profile = DomainProfile.Create("Administrator", "Old description", isSystem: true);

        var act = () => profile.Update("Administrator", "New description");

        act.Should().NotThrow();
        profile.Description.Should().Be("New description");
    }
}
