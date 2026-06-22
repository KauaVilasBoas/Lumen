using Lumen.Domain.Authorization;
using FluentAssertions;

namespace Lumen.UnitTests.Domain.Authorization;

public sealed class GroupPermissionTests
{
    [Fact]
    public void Create_WithValidArgs_ReturnsGroupPermission()
    {
        var group = GroupPermission.Create("User Management", "Permissions related to user operations");

        group.Name.Should().Be("User Management");
        group.Description.Should().Be("Permissions related to user operations");
        group.IsDeleted.Should().BeFalse();
        group.DeletedAt.Should().BeNull();
        group.Id.Should().NotBe(Guid.Empty);
    }

    [Theory]
    [InlineData("", "Description")]
    [InlineData("   ", "Description")]
    [InlineData("Name", "")]
    [InlineData("Name", "   ")]
    public void Create_WithBlankRequiredField_ThrowsArgumentException(string name, string description)
    {
        var act = () => GroupPermission.Create(name, description);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SoftDelete_SetsIsDeletedAndDeletedAt()
    {
        var group = GroupPermission.Create("Reports", "Report permissions");
        var before = DateTime.UtcNow;

        group.SoftDelete();

        group.IsDeleted.Should().BeTrue();
        group.DeletedAt.Should().NotBeNull().And.BeOnOrAfter(before);
    }
}
