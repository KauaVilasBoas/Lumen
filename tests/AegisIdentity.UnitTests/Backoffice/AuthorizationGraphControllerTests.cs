using System.Net;
using System.Security.Claims;
using System.Text.Json;
using AegisIdentity.Backoffice.Controllers;
using AegisIdentity.Backoffice.Services;
using AegisIdentity.Domain.Authorization;
using AegisIdentity.SharedKernel.Constants;
using AegisIdentity.UnitTests.Infrastructure.Security;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace AegisIdentity.UnitTests.Backoffice;

public sealed class AuthorizationGraphControllerTests
{
    private static readonly JsonSerializerOptions CamelCase = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly IUserPermissionService _permissionService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthorizationGraphControllerTests()
    {
        _permissionService = Substitute.For<IUserPermissionService>();
        _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
    }

    private AuthorizationGraphController BuildController(
        StubHttpMessageHandler httpHandler,
        Guid? userId = null)
    {
        var httpClient = new HttpClient(httpHandler) { BaseAddress = new Uri("http://api.test/") };
        var adminApiClient = new AdminApiClient(httpClient, _httpContextAccessor);

        var user = userId.HasValue
            ? new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString())],
                "cookie"))
            : new ClaimsPrincipal(new ClaimsIdentity());

        var controller = new AuthorizationGraphController(_permissionService, adminApiClient)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            }
        };

        return controller;
    }

    private static AdminApiClient.GraphSnapshot BuildSnapshot() =>
        new(
            Users: [new AdminApiClient.UserNode(
                Guid.NewGuid(), "alice", "alice@test.local", "active",
                ["profile-a"])],
            Profiles: new Dictionary<string, AdminApiClient.ProfileNode>
            {
                ["profile-a"] = new("Admins", IsSystem: true, Permissions: ["perm-1"])
            },
            Permissions: new Dictionary<string, AdminApiClient.PermissionNode>
            {
                ["perm-1"] = new("Users.Get", "View user", "Users", Orphan: false)
            });

    [Fact]
    public async Task Index_UserHasPermission_ReturnsViewWithGraphJson()
    {
        var userId = Guid.NewGuid();
        var snapshot = BuildSnapshot();
        var json = JsonSerializer.Serialize(snapshot, CamelCase);

        var handler = StubHttpMessageHandler.RespondingWith(HttpStatusCode.OK, json);
        handler.Requests.Clear();

        _permissionService
            .HasPermissionAsync(userId, PermissionCodes.AuthorizationGraph.View)
            .Returns(true);

        var sut = BuildController(handler, userId);

        var result = await sut.Index();

        result.Should().BeOfType<ViewResult>();
        var view = (ViewResult)result;
        view.ViewData["GraphJson"].Should().NotBeNull();

        var graphJson = view.ViewData["GraphJson"] as string;
        graphJson.Should().Contain("alice");
    }

    [Fact]
    public async Task Index_UserLacksPermission_ReturnsForbid()
    {
        var userId = Guid.NewGuid();
        var handler = StubHttpMessageHandler.RespondingWith(HttpStatusCode.OK, "{}");

        _permissionService
            .HasPermissionAsync(userId, PermissionCodes.AuthorizationGraph.View)
            .Returns(false);

        var sut = BuildController(handler, userId);

        var result = await sut.Index();

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task Index_AnonymousUser_ReturnsForbid()
    {
        var handler = StubHttpMessageHandler.RespondingWith(HttpStatusCode.OK, "{}");
        var sut = BuildController(handler, userId: null);

        var result = await sut.Index();

        result.Should().BeOfType<ForbidResult>();
        await _permissionService.DidNotReceive().HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Index_ApiReturnsError_SetsGraphJsonToNull()
    {
        var userId = Guid.NewGuid();
        var handler = StubHttpMessageHandler.RespondingWith(HttpStatusCode.ServiceUnavailable, string.Empty);

        _permissionService
            .HasPermissionAsync(userId, PermissionCodes.AuthorizationGraph.View)
            .Returns(true);

        var sut = BuildController(handler, userId);

        var result = await sut.Index();

        result.Should().BeOfType<ViewResult>();
        var view = (ViewResult)result;
        view.ViewData["GraphJson"].Should().BeNull();
    }

    [Fact]
    public async Task Index_PermissionCheckedWithCanonicalCode()
    {
        var userId = Guid.NewGuid();
        var handler = StubHttpMessageHandler.RespondingWith(HttpStatusCode.OK, "{}");

        _permissionService
            .HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<string>())
            .Returns(false);

        var sut = BuildController(handler, userId);

        await sut.Index();

        await _permissionService.Received(1)
            .HasPermissionAsync(userId, PermissionCodes.AuthorizationGraph.View);
    }
}
