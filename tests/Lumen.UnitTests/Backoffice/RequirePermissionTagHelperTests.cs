using System.Security.Claims;
using Lumen.Backoffice.TagHelpers;
using Lumen.Domain.Authorization;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Razor.TagHelpers;
using NSubstitute;

namespace Lumen.UnitTests.Backoffice;

public sealed class RequirePermissionTagHelperTests
{
    private readonly IUserPermissionService _permissionService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly RequirePermissionTagHelper _sut;

    public RequirePermissionTagHelperTests()
    {
        _permissionService = Substitute.For<IUserPermissionService>();
        _httpContextAccessor = Substitute.For<IHttpContextAccessor>();

        _sut = new RequirePermissionTagHelper(_permissionService, _httpContextAccessor);
    }

    private static TagHelperContext BuildContext()
        => new(
            tagName: "div",
            allAttributes: new TagHelperAttributeList(),
            items: new Dictionary<object, object>(),
            uniqueId: Guid.NewGuid().ToString());

    private static TagHelperOutput BuildOutput(string tagName = "div")
        => new(
            tagName,
            attributes: new TagHelperAttributeList(),
            getChildContentAsync: (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));

    private HttpContext BuildHttpContext(Guid? userId = null)
    {
        var context = Substitute.For<HttpContext>();
        var principal = userId.HasValue
            ? new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString())],
                "cookie"))
            : new ClaimsPrincipal(new ClaimsIdentity());

        context.User.Returns(principal);
        _httpContextAccessor.HttpContext.Returns(context);

        return context;
    }

    [Fact]
    public async Task ProcessAsync_UserHasPermission_DoesNotSuppressOutput()
    {
        var userId = Guid.NewGuid();
        BuildHttpContext(userId);
        _sut.Controller = "Admin";
        _sut.Action = "Ping";
        _permissionService.HasPermissionAsync(userId, "Admin.Ping").Returns(true);

        var output = BuildOutput();
        await _sut.ProcessAsync(BuildContext(), output);

        output.IsContentModified.Should().BeFalse();
        output.TagName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ProcessAsync_UserLacksPermission_SuppressesOutput()
    {
        var userId = Guid.NewGuid();
        BuildHttpContext(userId);
        _sut.Controller = "Admin";
        _sut.Action = "Ping";
        _permissionService.HasPermissionAsync(userId, "Admin.Ping").Returns(false);

        var output = BuildOutput();
        await _sut.ProcessAsync(BuildContext(), output);

        output.TagName.Should().BeNull();
    }

    [Fact]
    public async Task ProcessAsync_AnonymousUser_SuppressesOutput()
    {
        _httpContextAccessor.HttpContext.Returns((HttpContext?)null);
        _sut.Controller = "Admin";
        _sut.Action = "Ping";

        var output = BuildOutput();
        await _sut.ProcessAsync(BuildContext(), output);

        output.TagName.Should().BeNull();
        await _permissionService.DidNotReceive().HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<string>());
    }

    [Fact]
    public async Task ProcessAsync_ControllerNameNormalizedBeforeLookup()
    {
        var userId = Guid.NewGuid();
        BuildHttpContext(userId);
        _sut.Controller = "AdminController";
        _sut.Action = "Ping";
        _permissionService.HasPermissionAsync(userId, "Admin.Ping").Returns(true);

        var output = BuildOutput();
        await _sut.ProcessAsync(BuildContext(), output);

        await _permissionService.Received(1).HasPermissionAsync(userId, "Admin.Ping");
    }

    [Fact]
    public async Task ProcessAsync_EmptyControllerAttribute_SuppressesOutput()
    {
        var userId = Guid.NewGuid();
        BuildHttpContext(userId);
        _sut.Controller = "";
        _sut.Action = "Ping";

        var output = BuildOutput();
        await _sut.ProcessAsync(BuildContext(), output);

        output.TagName.Should().BeNull();
    }

    [Fact]
    public async Task ProcessAsync_PermissionAttributesRemovedFromOutput()
    {
        var userId = Guid.NewGuid();
        BuildHttpContext(userId);
        _sut.Controller = "Admin";
        _sut.Action = "Ping";
        _permissionService.HasPermissionAsync(userId, "Admin.Ping").Returns(true);

        var output = BuildOutput();
        output.Attributes.Add("asp-require-permission-controller", "Admin");
        output.Attributes.Add("asp-require-permission-action", "Ping");
        output.Attributes.Add("class", "btn");

        await _sut.ProcessAsync(BuildContext(), output);

        output.Attributes.Should().NotContain(a => a.Name == "asp-require-permission-controller");
        output.Attributes.Should().NotContain(a => a.Name == "asp-require-permission-action");
        output.Attributes.Should().Contain(a => a.Name == "class");
    }
}
