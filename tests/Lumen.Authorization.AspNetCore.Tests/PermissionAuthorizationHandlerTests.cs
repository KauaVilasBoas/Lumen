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

public sealed class PermissionAuthorizationHandlerTests
{
    private readonly IUserPermissionService _permissionService;
    private readonly IAuthorizationHandler _handler;

    public PermissionAuthorizationHandlerTests()
    {
        _permissionService = Substitute.For<IUserPermissionService>();
        var accessor = new ClaimsUserIdAccessor(Options.Create(new LumenAuthorizationOptions()));
        _handler = new PermissionAuthorizationHandler(_permissionService, accessor);
    }

    [Fact]
    public async Task HandleRequirement_WithExplicitCode_UserHasPermission_Succeeds()
    {
        var userId = Guid.NewGuid();
        var requirement = new PermissionRequirement("Users.List");
        var context = BuildContext(userId, requirement, httpContext: null);

        _permissionService.HasPermissionAsync(userId, "Users.List", Arg.Any<CancellationToken>())
            .Returns(true);

        await _handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleRequirement_WithExplicitCode_UserLacksPermission_DoesNotSucceed()
    {
        var userId = Guid.NewGuid();
        var requirement = new PermissionRequirement("Users.List");
        var context = BuildContext(userId, requirement, httpContext: null);

        _permissionService.HasPermissionAsync(userId, "Users.List", Arg.Any<CancellationToken>())
            .Returns(false);

        await _handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleRequirement_WithoutCode_UsesControllerActionConvention_Succeeds()
    {
        var userId = Guid.NewGuid();
        var requirement = new PermissionRequirement();
        var httpContext = BuildHttpContextWithActionDescriptor<FakeUsersController>(nameof(FakeUsersController.List));
        var context = BuildContext(userId, requirement, httpContext);

        _permissionService.HasPermissionAsync(userId, "FakeUsers.List", Arg.Any<CancellationToken>())
            .Returns(true);

        await _handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleRequirement_WithoutCode_UsesControllerActionConvention_UserLacksPermission_DoesNotSucceed()
    {
        var userId = Guid.NewGuid();
        var requirement = new PermissionRequirement();
        var httpContext = BuildHttpContextWithActionDescriptor<FakeDiagnosticsController>(
            nameof(FakeDiagnosticsController.GetCacheStats));
        var context = BuildContext(userId, requirement, httpContext);

        _permissionService.HasPermissionAsync(userId, "FakeDiagnostics.GetCacheStats", Arg.Any<CancellationToken>())
            .Returns(false);

        await _handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleRequirement_MissingSubClaim_DoesNotCallPermissionService()
    {
        var requirement = new PermissionRequirement("Users.List");
        var user = new ClaimsPrincipal(new ClaimsIdentity());
        var authContext = new AuthorizationHandlerContext([requirement], user, resource: null);

        await _handler.HandleAsync(authContext);

        await _permissionService.DidNotReceive()
            .HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        authContext.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleRequirement_InvalidSubClaim_DoesNotCallPermissionService()
    {
        var requirement = new PermissionRequirement("Users.List");
        var user = BuildUserWithSubject("not-a-guid");
        var authContext = new AuthorizationHandlerContext([requirement], user, resource: null);

        await _handler.HandleAsync(authContext);

        await _permissionService.DidNotReceive()
            .HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleRequirement_WithoutCodeAndNoHttpContext_DoesNotSucceed()
    {
        var userId = Guid.NewGuid();
        var requirement = new PermissionRequirement();
        var context = BuildContext(userId, requirement, httpContext: null);

        await _handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
        await _permissionService.DidNotReceive()
            .HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleRequirement_CustomClaimType_ValidId_Succeeds()
    {
        const string customClaimType = "uid";
        var options = Options.Create(new LumenAuthorizationOptions { UserIdClaimType = customClaimType });
        var accessor = new ClaimsUserIdAccessor(options);
        var permissionService = Substitute.For<IUserPermissionService>();
        var handler = new PermissionAuthorizationHandler(permissionService, accessor);

        var userId = Guid.NewGuid();
        var identity = new ClaimsIdentity([new Claim(customClaimType, userId.ToString())]);
        var user = new ClaimsPrincipal(identity);
        var requirement = new PermissionRequirement("Users.List");
        var context = new AuthorizationHandlerContext([requirement], user, resource: null);

        permissionService.HasPermissionAsync(userId, "Users.List", Arg.Any<CancellationToken>())
            .Returns(true);

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleRequirement_DefaultClaimAbsent_WhenCustomClaimConfigured_DoesNotSucceed()
    {
        const string customClaimType = "uid";
        var options = Options.Create(new LumenAuthorizationOptions { UserIdClaimType = customClaimType });
        var accessor = new ClaimsUserIdAccessor(options);
        var permissionService = Substitute.For<IUserPermissionService>();
        var handler = new PermissionAuthorizationHandler(permissionService, accessor);

        var userId = Guid.NewGuid();
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId.ToString())]);
        var user = new ClaimsPrincipal(identity);
        var requirement = new PermissionRequirement("Users.List");
        var context = new AuthorizationHandlerContext([requirement], user, resource: null);

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse(
            because: "the handler must read from the configured claim type only");
        await permissionService.DidNotReceive()
            .HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    private static AuthorizationHandlerContext BuildContext(
        Guid userId,
        PermissionRequirement requirement,
        HttpContext? httpContext)
    {
        var user = BuildUserWithSubject(userId.ToString());
        return new AuthorizationHandlerContext([requirement], user, resource: httpContext);
    }

    private static ClaimsPrincipal BuildUserWithSubject(string subject)
    {
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, subject)]);
        return new ClaimsPrincipal(identity);
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

    private sealed class FakeUsersController
    {
        public void List() { }
    }

    private sealed class FakeDiagnosticsController
    {
        public void GetCacheStats() { }
    }
}
