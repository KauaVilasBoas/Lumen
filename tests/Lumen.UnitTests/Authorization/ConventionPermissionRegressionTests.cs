using System.Reflection;
using Lumen.Api.Controllers;
using Lumen.SharedKernel.Constants;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using NSubstitute;
using Lumen.Authorization.AspNetCore;

namespace Lumen.UnitTests.Authorization;

public sealed class ConventionPermissionRegressionTests
{
    [Fact]
    public void Scan_UsersController_GetAction_ProducesUsersGetCode()
    {
        var descriptor = BuildDescriptor<UsersController>(nameof(UsersController.Get));
        var provider   = BuildProvider([descriptor]);
        var scanner    = new PermissionDiscoveryScanner(provider);

        var result = scanner.Scan();

        result.Should().HaveCount(1);
        result[0].Code.Should().Be(PermissionCodes.Users.Get);
        result[0].Controller.Should().Be("Users");
        result[0].Action.Should().Be("Get");
    }

    [Fact]
    public void Scan_AuditController_ReadAction_ProducesAuditReadCode()
    {
        var descriptor = BuildDescriptor<AuditController>(nameof(AuditController.Read));
        var provider   = BuildProvider([descriptor]);
        var scanner    = new PermissionDiscoveryScanner(provider);

        var result = scanner.Scan();

        result.Should().HaveCount(1);
        result[0].Code.Should().Be(PermissionCodes.Audit.Read);
        result[0].Controller.Should().Be("Audit");
        result[0].Action.Should().Be("Read");
    }

    [Fact]
    public void Scan_DiagnosticsController_GetCacheStatsAction_ProducesDiagnosticsGetCacheStatsCode()
    {
        var descriptor = BuildDescriptor<DiagnosticsController>(nameof(DiagnosticsController.GetCacheStats));
        var provider   = BuildProvider([descriptor]);
        var scanner    = new PermissionDiscoveryScanner(provider);

        var result = scanner.Scan();

        result.Should().HaveCount(1);
        result[0].Code.Should().Be(PermissionCodes.Diagnostics.GetCacheStats);
        result[0].Controller.Should().Be("Diagnostics");
        result[0].Action.Should().Be("GetCacheStats");
    }

    [Fact]
    public void Scan_DiagnosticsController_GetJobStatsAction_ProducesDiagnosticsGetJobStatsCode()
    {
        var descriptor = BuildDescriptor<DiagnosticsController>(nameof(DiagnosticsController.GetJobStats));
        var provider   = BuildProvider([descriptor]);
        var scanner    = new PermissionDiscoveryScanner(provider);

        var result = scanner.Scan();

        result.Should().HaveCount(1);
        result[0].Code.Should().Be(PermissionCodes.Diagnostics.GetJobStats);
        result[0].Controller.Should().Be("Diagnostics");
        result[0].Action.Should().Be("GetJobStats");
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
        var method         = controllerType.GetMethod(methodName)!;

        return new ControllerActionDescriptor
        {
            ControllerTypeInfo = controllerType,
            MethodInfo         = method,
            ControllerName     = controllerType.Name,
            ActionName         = methodName,
        };
    }
}
