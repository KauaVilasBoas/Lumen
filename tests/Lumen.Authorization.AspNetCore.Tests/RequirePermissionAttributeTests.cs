using FluentAssertions;
using Lumen.Authorization.AspNetCore;
using Microsoft.AspNetCore.Authorization;

namespace Lumen.Authorization.AspNetCore.Tests;

public sealed class RequirePermissionAttributeTests
{
    [Fact]
    public void GetRequirements_WithExplicitCode_ReturnsRequirementWithCode()
    {
        var attribute = new RequirePermissionAttribute("Users.List");

        var requirements = attribute.GetRequirements().ToList();

        requirements.Should().HaveCount(1);
        requirements[0].Should().BeOfType<PermissionRequirement>()
            .Which.Code.Should().Be("Users.List");
    }

    [Fact]
    public void GetRequirements_WithoutCode_ReturnsRequirementWithNullCode()
    {
        var attribute = new RequirePermissionAttribute();

        var requirements = attribute.GetRequirements().ToList();

        requirements.Should().HaveCount(1);
        requirements[0].Should().BeOfType<PermissionRequirement>()
            .Which.Code.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithBlankCode_ThrowsArgumentException()
    {
        var act = () => new RequirePermissionAttribute("   ");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Attribute_ImplementsIAuthorizationRequirementData()
    {
        var attribute = new RequirePermissionAttribute("Permissions.List");

        attribute.Should().BeAssignableTo<IAuthorizationRequirementData>();
    }

    [Fact]
    public void Attribute_ImplementsIAuthorizeData()
    {
        var attribute = new RequirePermissionAttribute("Users.Delete");

        attribute.Should().BeAssignableTo<IAuthorizeData>();
    }
}
