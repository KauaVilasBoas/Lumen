using System.Reflection;
using System.Security.Claims;
using FluentAssertions;
using Lumen.Authorization.AspNetCore;
using Lumen.Authorization.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Lumen.Authorization.AspNetCore.Tests;

public sealed class LumenDefaultPolicyConventionTests
{
    private readonly IUserPermissionService _permissionService;
    private readonly IAuthorizationHandler _handler;

    public LumenDefaultPolicyConventionTests()
    {
        _permissionService = Substitute.For<IUserPermissionService>();
        var accessor = new ClaimsUserIdAccessor(Options.Create(new LumenAuthorizationOptions()));
        _handler = new PermissionAuthorizationHandler(_permissionService, accessor);
    }

    [Fact]
    public async Task LumenDefaultPolicy_ResolvesConventionCode_WhenUserHasPermission_Succeeds()
    {
        var userId = Guid.NewGuid();
        var requirement = new PermissionRequirement(code: null);
        var httpContext = BuildHttpContextWithActionDescriptor<FakeOrdersController>(
            nameof(FakeOrdersController.Index));
        var context = BuildContext(userId, requirement, httpContext);

        _permissionService.HasPermissionAsync(userId, "FakeOrders.Index", Arg.Any<CancellationToken>())
            .Returns(true);

        await _handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task LumenDefaultPolicy_ResolvesConventionCode_WhenUserLacksPermission_DoesNotSucceed()
    {
        var userId = Guid.NewGuid();
        var requirement = new PermissionRequirement(code: null);
        var httpContext = BuildHttpContextWithActionDescriptor<FakeOrdersController>(
            nameof(FakeOrdersController.Index));
        var context = BuildContext(userId, requirement, httpContext);

        _permissionService.HasPermissionAsync(userId, "FakeOrders.Index", Arg.Any<CancellationToken>())
            .Returns(false);

        await _handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public void LumenDefaultPolicyConstant_ValueMatchesExpectedString()
    {
        LumenPolicy.Default.Should().Be("Lumen");
    }

    [Fact]
    public async Task LumenDefaultPolicy_ResolvedByProvider_ContainsConventionalRequirement()
    {
        var options = Options.Create(new AuthorizationOptions());
        var provider = new PermissionPolicyProvider(options);

        var policy = await provider.GetPolicyAsync(LumenPolicy.Default);

        policy.Should().NotBeNull();
        policy!.Requirements.OfType<PermissionRequirement>().Should()
            .ContainSingle(req => req.Code == null,
                because: "LumenPolicy.Default triggers convention-based resolution with null code");
    }

    private static AuthorizationHandlerContext BuildContext(
        Guid userId,
        PermissionRequirement requirement,
        HttpContext httpContext)
    {
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId.ToString())]);
        var user = new ClaimsPrincipal(identity);
        return new AuthorizationHandlerContext([requirement], user, resource: httpContext);
    }

    private static HttpContext BuildHttpContextWithActionDescriptor<TController>(string methodName)
    {
        var httpContext = new DefaultHttpContext();

        var controllerType = typeof(TController).GetTypeInfo();
        var methodInfo = controllerType.GetMethod(methodName)!;

        var actionDescriptor = new ControllerActionDescriptor
        {
            ControllerTypeInfo = controllerType,
            MethodInfo = methodInfo,
            ControllerName = controllerType.Name,
            ActionName = methodName,
        };

        var endpointMetadata = new EndpointMetadataCollection(actionDescriptor);
        var endpoint = new Endpoint(
            requestDelegate: _ => Task.CompletedTask,
            metadata: endpointMetadata,
            displayName: null);

        httpContext.SetEndpoint(endpoint);

        return httpContext;
    }

    private sealed class FakeOrdersController
    {
        public void Index() { }
    }
}
