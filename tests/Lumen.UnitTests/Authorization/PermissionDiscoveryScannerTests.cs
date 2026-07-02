using System.Reflection;
using Lumen.Api.Authorization;
using Lumen.Authorization.AspNetCore;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using NSubstitute;

namespace Lumen.UnitTests.Authorization;

public sealed class PermissionDiscoveryScannerTests
{
    private static IActionDescriptorCollectionProvider BuildProvider(
        IReadOnlyList<ActionDescriptor> descriptors)
    {
        var provider = Substitute.For<IActionDescriptorCollectionProvider>();
        provider.ActionDescriptors.Returns(
            new ActionDescriptorCollection(descriptors, version: 1));

        return provider;
    }

    private static ControllerActionDescriptor BuildDescriptor<TController>(string methodName)
    {
        var controllerType = typeof(TController).GetTypeInfo();
        var method = controllerType.GetMethod(methodName)!;

        return new ControllerActionDescriptor
        {
            ControllerTypeInfo = controllerType,
            MethodInfo = method,
            ControllerName = controllerType.Name,
            ActionName = methodName,
        };
    }

    [Fact]
    public void Scan_MethodWithRequirePermission_ReturnsDiscoveredPermission()
    {
        var descriptor = BuildDescriptor<UsersController>(nameof(UsersController.Delete));
        var provider = BuildProvider([descriptor]);
        var scanner = new PermissionDiscoveryScanner(provider);

        var result = scanner.Scan();

        result.Should().HaveCount(1);
        result[0].Code.Should().Be("Users.Delete");
        result[0].Controller.Should().Be("Users");
        result[0].Action.Should().Be("Delete");
        result[0].GroupName.Should().Be("Users");
    }

    [Fact]
    public void Scan_ControllerWithRequirePermission_IncludesAllActions()
    {
        var descriptors = new List<ActionDescriptor>
        {
            BuildDescriptor<AnnotatedController>(nameof(AnnotatedController.List)),
            BuildDescriptor<AnnotatedController>(nameof(AnnotatedController.Get)),
        };
        var provider = BuildProvider(descriptors);
        var scanner = new PermissionDiscoveryScanner(provider);

        var result = scanner.Scan();

        result.Should().HaveCount(2);
        result.Should().Contain(p => p.Code == "Annotated.List");
        result.Should().Contain(p => p.Code == "Annotated.Get");
    }

    [Fact]
    public void Scan_ActionWithExplicitCodeOverride_UsesOverrideCode()
    {
        var descriptor = BuildDescriptor<UsersController>(nameof(UsersController.Create));
        var provider = BuildProvider([descriptor]);
        var scanner = new PermissionDiscoveryScanner(provider);

        var result = scanner.Scan();

        result.Should().HaveCount(1);
        result[0].Code.Should().Be("custom.create.override");
    }

    [Fact]
    public void Scan_ControllerWithPermissionGroup_UsesGroupName()
    {
        var descriptor = BuildDescriptor<GroupedController>(nameof(GroupedController.Index));
        var provider = BuildProvider([descriptor]);
        var scanner = new PermissionDiscoveryScanner(provider);

        var result = scanner.Scan();

        result.Should().HaveCount(1);
        result[0].GroupName.Should().Be("Gestão de Usuários");
    }

    [Fact]
    public void Scan_ActionWithoutRequirePermission_IsExcluded()
    {
        var descriptor = BuildDescriptor<UsersController>(nameof(UsersController.Public));
        var provider = BuildProvider([descriptor]);
        var scanner = new PermissionDiscoveryScanner(provider);

        var result = scanner.Scan();

        result.Should().BeEmpty();
    }

    [Fact]
    public void Scan_ControllerNameNormalizationRemovesSuffix()
    {
        var descriptor = BuildDescriptor<UsersController>(nameof(UsersController.Delete));
        var provider = BuildProvider([descriptor]);
        var scanner = new PermissionDiscoveryScanner(provider);

        var result = scanner.Scan();

        result[0].Controller.Should().Be("Users");
        result[0].Code.Should().NotContain("Controller");
    }

    [Fact]
    public void Scan_EmptyDescriptors_ReturnsEmpty()
    {
        var provider = BuildProvider([]);
        var scanner = new PermissionDiscoveryScanner(provider);

        var result = scanner.Scan();

        result.Should().BeEmpty();
    }

    private sealed class UsersController
    {
        [RequirePermission]
        public void Delete() { }

        [RequirePermission("custom.create.override")]
        public void Create() { }

        public void Public() { }
    }

    [RequirePermission]
    private sealed class AnnotatedController
    {
        public void List() { }

        public void Get() { }
    }

    [PermissionGroup("Gestão de Usuários")]
    private sealed class GroupedController
    {
        [RequirePermission]
        public void Index() { }
    }
}
