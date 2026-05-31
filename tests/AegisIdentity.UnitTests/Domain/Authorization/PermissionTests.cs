using AegisIdentity.Domain.Authorization;
using FluentAssertions;

namespace AegisIdentity.UnitTests.Domain.Authorization;

public sealed class PermissionTests
{
    [Fact]
    public void BuildCode_ReturnsDotSeparatedControllerAndAction()
    {
        var code = Permission.BuildCode("Users", "GetById");

        code.Should().Be("Users.GetById");
    }

    [Theory]
    [InlineData("", "GetById")]
    [InlineData("   ", "GetById")]
    [InlineData("Users", "")]
    [InlineData("Users", "   ")]
    public void BuildCode_WithBlankSegment_ThrowsArgumentException(string controller, string action)
    {
        var act = () => Permission.BuildCode(controller, action);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithValidArgs_SetsCodeFromControllerAndAction()
    {
        var permission = Permission.Create("Orders", "List", "List Orders");

        permission.Controller.Should().Be("Orders");
        permission.Action.Should().Be("List");
        permission.Code.Should().Be("Orders.List");
        permission.DisplayName.Should().Be("List Orders");
        permission.GroupPermissionId.Should().BeNull();
    }

    [Fact]
    public void Create_WithGroupPermissionId_SetsGroupPermissionId()
    {
        var groupId = Guid.NewGuid();

        var permission = Permission.Create("Orders", "Delete", "Delete Order", groupId);

        permission.GroupPermissionId.Should().Be(groupId);
    }

    [Theory]
    [InlineData("", "Action", "DisplayName")]
    [InlineData("   ", "Action", "DisplayName")]
    [InlineData("Controller", "", "DisplayName")]
    [InlineData("Controller", "   ", "DisplayName")]
    [InlineData("Controller", "Action", "")]
    [InlineData("Controller", "Action", "   ")]
    public void Create_WithBlankRequiredField_ThrowsArgumentException(string controller, string action, string displayName)
    {
        var act = () => Permission.Create(controller, action, displayName);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_SetsNewGuidId()
    {
        var permission = Permission.Create("Users", "Create", "Create User");

        permission.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_IsNotDeleted()
    {
        var permission = Permission.Create("Users", "Create", "Create User");

        permission.IsDeleted.Should().BeFalse();
        permission.DeletedAt.Should().BeNull();
    }

    [Fact]
    public void SoftDelete_SetsIsDeletedAndDeletedAt()
    {
        var permission = Permission.Create("Users", "Create", "Create User");
        var before = DateTime.UtcNow;

        permission.SoftDelete();

        permission.IsDeleted.Should().BeTrue();
        permission.DeletedAt.Should().NotBeNull().And.BeOnOrAfter(before);
    }

    [Fact]
    public void MarkAsOrphan_SetsIsOrphanAndOrphanedAt()
    {
        var permission = Permission.Create("Users", "Create", "Create User");
        var before = DateTime.UtcNow;

        permission.MarkAsOrphan();

        permission.IsOrphan.Should().BeTrue();
        permission.OrphanedAt.Should().NotBeNull().And.BeOnOrAfter(before);
    }

    [Fact]
    public void ClearOrphan_ResetsIsOrphanAndOrphanedAt()
    {
        var permission = Permission.Create("Users", "Create", "Create User");
        permission.MarkAsOrphan();

        permission.ClearOrphan();

        permission.IsOrphan.Should().BeFalse();
        permission.OrphanedAt.Should().BeNull();
    }

    [Fact]
    public void Update_ChangesControllerActionDisplayNameAndGroup()
    {
        var permission = Permission.Create("OldController", "OldAction", "Old Display");
        var groupId = Guid.NewGuid();

        permission.Update("NewController", "NewAction", "New Display", groupId);

        permission.Controller.Should().Be("NewController");
        permission.Action.Should().Be("NewAction");
        permission.DisplayName.Should().Be("New Display");
        permission.GroupPermissionId.Should().Be(groupId);
    }

    [Theory]
    [InlineData("", "Action", "DisplayName")]
    [InlineData("   ", "Action", "DisplayName")]
    [InlineData("Controller", "", "DisplayName")]
    [InlineData("Controller", "   ", "DisplayName")]
    [InlineData("Controller", "Action", "")]
    [InlineData("Controller", "Action", "   ")]
    public void Update_WithBlankRequiredField_ThrowsArgumentException(string controller, string action, string displayName)
    {
        var permission = Permission.Create("Users", "Delete", "Delete User");

        var act = () => permission.Update(controller, action, displayName, null);

        act.Should().Throw<ArgumentException>();
    }
}
