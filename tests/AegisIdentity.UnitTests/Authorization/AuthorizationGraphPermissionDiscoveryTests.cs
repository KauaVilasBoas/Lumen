using System.Reflection;
using AegisIdentity.Api.Authorization;
using AegisIdentity.Api.Controllers;
using AegisIdentity.SharedKernel.Constants;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using NSubstitute;

namespace AegisIdentity.UnitTests.Authorization;

public sealed class AuthorizationGraphPermissionDiscoveryTests
{
    [Fact]
    public void Scan_AuthorizationGraphController_DiscoverGetPermissionWithCorrectCode()
    {
        var descriptor = BuildDescriptor<AuthorizationGraphController>(nameof(AuthorizationGraphController.Get));
        var provider = BuildProvider([descriptor]);
        var scanner = new PermissionDiscoveryScanner(provider);

        var result = scanner.Scan();

        result.Should().HaveCount(1);
        result[0].Code.Should().Be(PermissionCodes.AuthorizationGraph.View);
    }

    [Fact]
    public void Scan_AuthorizationGraphController_DiscoverGetPermissionWithAuthorizationGroup()
    {
        var descriptor = BuildDescriptor<AuthorizationGraphController>(nameof(AuthorizationGraphController.Get));
        var provider = BuildProvider([descriptor]);
        var scanner = new PermissionDiscoveryScanner(provider);

        var result = scanner.Scan();

        result.Should().HaveCount(1);
        result[0].GroupName.Should().Be(PermissionGroups.Authorization);
    }

    [Fact]
    public void Scan_AuthorizationGraphController_DiscoverGetPermissionWithCorrectControllerAndAction()
    {
        var descriptor = BuildDescriptor<AuthorizationGraphController>(nameof(AuthorizationGraphController.Get));
        var provider = BuildProvider([descriptor]);
        var scanner = new PermissionDiscoveryScanner(provider);

        var result = scanner.Scan();

        result.Should().HaveCount(1);
        result[0].Controller.Should().Be("AuthorizationGraph");
        result[0].Action.Should().Be("Get");
    }

    private static IActionDescriptorCollectionProvider BuildProvider(IReadOnlyList<ActionDescriptor> descriptors)
    {
        var provider = Substitute.For<IActionDescriptorCollectionProvider>();
        provider.ActionDescriptors.Returns(new ActionDescriptorCollection(descriptors, version: 1));
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
}
