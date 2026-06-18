using System.Net;
using System.Text.Json;
using AegisIdentity.Backoffice.Controllers;
using AegisIdentity.Backoffice.Services;
using AegisIdentity.Backoffice.ViewModels;
using AegisIdentity.UnitTests.Infrastructure.Security;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using NSubstitute;

namespace AegisIdentity.UnitTests.Backoffice;

public sealed class ProfilesControllerTests
{
    private static readonly JsonSerializerOptions CamelCase = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly IHttpContextAccessor _httpContextAccessor;

    public ProfilesControllerTests()
    {
        _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        _httpContextAccessor.HttpContext.Returns((HttpContext?)null);
    }

    private ProfilesController BuildController(StubHttpMessageHandler httpHandler)
    {
        var httpClient = new HttpClient(httpHandler) { BaseAddress = new Uri("http://api.test/") };
        var adminApiClient = new AdminApiClient(httpClient, _httpContextAccessor);

        var httpContext = new DefaultHttpContext();
        var tempDataProvider = Substitute.For<ITempDataProvider>();
        tempDataProvider.LoadTempData(Arg.Any<HttpContext>()).Returns(new Dictionary<string, object?>());

        return new ProfilesController(adminApiClient)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            },
            TempData = new TempDataDictionary(httpContext, tempDataProvider)
        };
    }

    private static Task<HttpResponseMessage> JsonOk<T>(T payload)
        => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(payload, CamelCase),
                System.Text.Encoding.UTF8,
                "application/json")
        });

    private static AdminApiClient.ProfileItem ProfileItem(Guid? id = null, bool isSystem = false)
        => new(id ?? Guid.NewGuid(), "Test Profile", "A test profile", isSystem);

    private static AdminApiClient.ProfileDetail ProfileDetail(Guid? id = null, bool isSystem = false)
        => new(id ?? Guid.NewGuid(), "Test Profile", "A test profile", isSystem, []);

    private static IReadOnlyList<AdminApiClient.PermissionGroup> SamplePermissionGroups()
        =>
        [
            new(Guid.NewGuid(), "Users", [
                new(Guid.NewGuid(), "Users.List", "List Users", false),
                new(Guid.NewGuid(), "Users.Get", "Get User", false)
            ])
        ];

    [Fact]
    public async Task Index_ApiReturnsProfiles_ReturnsViewWithProfileList()
    {
        var profiles = new List<AdminApiClient.ProfileItem>
        {
            ProfileItem(),
            ProfileItem(isSystem: true)
        };

        var handler = new StubHttpMessageHandler((req, _) =>
            req.RequestUri!.PathAndQuery.StartsWith("/api/profiles")
                ? JsonOk(profiles)
                : Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));

        var sut = BuildController(handler);
        var result = await sut.Index(CancellationToken.None);

        result.Should().BeOfType<ViewResult>();
        var view = (ViewResult)result;
        var model = view.Model as IReadOnlyList<AdminApiClient.ProfileItem>;
        model.Should().HaveCount(2);
    }

    [Fact]
    public async Task Index_ApiReturnsNull_ReturnsViewWithEmptyList()
    {
        var handler = StubHttpMessageHandler.RespondingWith(HttpStatusCode.ServiceUnavailable, string.Empty);
        var sut = BuildController(handler);

        var result = await sut.Index(CancellationToken.None);

        result.Should().BeOfType<ViewResult>();
        var view = (ViewResult)result;
        var model = view.Model as IReadOnlyList<AdminApiClient.ProfileItem>;
        model.Should().BeEmpty();
    }

    [Fact]
    public async Task Details_ProfileExists_ReturnsViewWithProfileAndPermissions()
    {
        var profileId = Guid.NewGuid();
        var profile = ProfileDetail(profileId);
        var groups = SamplePermissionGroups();

        var handler = new StubHttpMessageHandler((req, _) =>
        {
            var path = req.RequestUri!.PathAndQuery;
            if (path == $"/api/profiles/{profileId}") return JsonOk(profile);
            if (path.StartsWith("/api/permissions")) return JsonOk(groups);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        });

        var sut = BuildController(handler);
        var result = await sut.Details(profileId, CancellationToken.None);

        result.Should().BeOfType<ViewResult>();
        var view = (ViewResult)result;
        var vm = view.Model.Should().BeOfType<ProfileDetailViewModel>().Subject;
        vm.Profile.Should().BeEquivalentTo(profile);
        vm.AllPermissions.Should().NotBeNull();
    }

    [Fact]
    public async Task Details_ProfileNotFound_ReturnsNotFound()
    {
        var handler = StubHttpMessageHandler.RespondingWith(HttpStatusCode.NotFound, string.Empty);
        var sut = BuildController(handler);

        var result = await sut.Details(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Details_PermissionsApiUnavailable_AllPermissionsIsEmpty()
    {
        var profileId = Guid.NewGuid();
        var profile = ProfileDetail(profileId);

        var handler = new StubHttpMessageHandler((req, _) =>
        {
            var path = req.RequestUri!.PathAndQuery;
            if (path == $"/api/profiles/{profileId}") return JsonOk(profile);
            if (path.StartsWith("/api/permissions"))
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        });

        var sut = BuildController(handler);
        var result = await sut.Details(profileId, CancellationToken.None);

        result.Should().BeOfType<ViewResult>();
        var view = (ViewResult)result;
        var vm = view.Model.Should().BeOfType<ProfileDetailViewModel>().Subject;
        vm.AllPermissions.Should().BeEmpty();
    }

    [Fact]
    public async Task Create_ValidForm_RedirectsToIndex()
    {
        var handler = StubHttpMessageHandler.RespondingWith(HttpStatusCode.OK, string.Empty);
        var sut = BuildController(handler);
        var form = new ProfilesController.CreateProfileFormModel("New Profile", "A description");

        var result = await sut.Create(form, CancellationToken.None);

        result.Should().BeOfType<RedirectToActionResult>();
        ((RedirectToActionResult)result).ActionName.Should().Be(nameof(ProfilesController.Index));
    }

    [Fact]
    public async Task Create_ApiReturnsError_ReturnsViewWithModelError()
    {
        var handler = StubHttpMessageHandler.RespondingWith(HttpStatusCode.BadRequest, "Name already taken");
        var sut = BuildController(handler);
        var form = new ProfilesController.CreateProfileFormModel("Duplicate", "");

        var result = await sut.Create(form, CancellationToken.None);

        result.Should().BeOfType<ViewResult>();
        sut.ModelState.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Edit_SystemProfile_ReturnsForbid()
    {
        var profileId = Guid.NewGuid();
        var systemProfile = ProfileDetail(profileId, isSystem: true);

        var handler = new StubHttpMessageHandler((req, _) =>
            req.RequestUri!.PathAndQuery == $"/api/profiles/{profileId}"
                ? JsonOk(systemProfile)
                : Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));

        var sut = BuildController(handler);
        var result = await sut.Edit(profileId, CancellationToken.None);

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task Edit_NonSystemProfile_ReturnsView()
    {
        var profileId = Guid.NewGuid();
        var profile = ProfileDetail(profileId, isSystem: false);

        var handler = new StubHttpMessageHandler((req, _) =>
            req.RequestUri!.PathAndQuery == $"/api/profiles/{profileId}"
                ? JsonOk(profile)
                : Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));

        var sut = BuildController(handler);
        var result = await sut.Edit(profileId, CancellationToken.None);

        result.Should().BeOfType<ViewResult>();
    }

    [Fact]
    public async Task Edit_ProfileNotFound_ReturnsNotFound()
    {
        var handler = StubHttpMessageHandler.RespondingWith(HttpStatusCode.NotFound, string.Empty);
        var sut = BuildController(handler);

        var result = await sut.Edit(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Delete_ApiSucceeds_RedirectsToIndex()
    {
        var handler = StubHttpMessageHandler.RespondingWith(HttpStatusCode.OK, string.Empty);
        var sut = BuildController(handler);

        var result = await sut.Delete(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<RedirectToActionResult>();
        ((RedirectToActionResult)result).ActionName.Should().Be(nameof(ProfilesController.Index));
    }

    [Fact]
    public async Task Delete_ApiReturnsError_SetsErrorTempDataAndRedirects()
    {
        var handler = StubHttpMessageHandler.RespondingWith(HttpStatusCode.BadRequest, "Cannot delete system profile");
        var sut = BuildController(handler);

        var result = await sut.Delete(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<RedirectToActionResult>();
        sut.TempData["Error"].Should().NotBeNull();
    }

    [Fact]
    public async Task SetPermissions_ApiSucceeds_RedirectsToDetails()
    {
        var profileId = Guid.NewGuid();
        var handler = StubHttpMessageHandler.RespondingWith(HttpStatusCode.OK, string.Empty);
        var sut = BuildController(handler);

        var result = await sut.SetPermissions(profileId, [Guid.NewGuid()], CancellationToken.None);

        result.Should().BeOfType<RedirectToActionResult>();
        var redirect = (RedirectToActionResult)result;
        redirect.ActionName.Should().Be(nameof(ProfilesController.Details));
        redirect.RouteValues!["id"].Should().Be(profileId);
    }

    [Fact]
    public async Task SetPermissions_ApiReturnsError_SetsErrorTempDataAndRedirects()
    {
        var handler = StubHttpMessageHandler.RespondingWith(HttpStatusCode.BadRequest, "Forbidden");
        var sut = BuildController(handler);

        var result = await sut.SetPermissions(Guid.NewGuid(), [], CancellationToken.None);

        result.Should().BeOfType<RedirectToActionResult>();
        sut.TempData["Error"].Should().NotBeNull();
    }
}
