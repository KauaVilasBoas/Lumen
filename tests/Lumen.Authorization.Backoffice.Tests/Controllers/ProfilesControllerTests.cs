using FluentAssertions;
using Lumen.Authorization.Application.Profiles.Create;
using Lumen.Authorization.Application.Profiles.Delete;
using Lumen.Authorization.Application.Profiles.SetPermissions;
using Lumen.Authorization.Application.Profiles.Update;
using Lumen.Authorization.Application.Queries;
using Lumen.Authorization.Backoffice.Areas.Lumen.Controllers;
using Lumen.Authorization.Backoffice.ViewModels;
using Lumen.Authorization.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Lumen.Authorization.Backoffice.Tests.Controllers;

public sealed class ProfilesControllerTests
{
    private readonly ISender _sender;
    private readonly ProfilesController _controller;
    private readonly FakeTempData _tempData;

    public ProfilesControllerTests()
    {
        _sender = Substitute.For<ISender>();
        _tempData = new FakeTempData();
        _controller = new ProfilesController(_sender);
        _controller.TempData = _tempData;
    }

    private sealed class FakeTempData : Dictionary<string, object?>, ITempDataDictionary
    {
        public void Keep() { }
        public void Keep(string key) { }
        public void Load() { }
        public void Save() { }
        public object? Peek(string key) => TryGetValue(key, out var v) ? v : null;
    }

    [Fact]
    public async Task Index_ReturnsViewWithProfileListViewModel()
    {
        var profiles = new List<ListProfilesResult>
        {
            new(Guid.NewGuid(), "Admin", "Administrator profile", IsSystem: true),
            new(Guid.NewGuid(), "Auditor", "Read-only access", IsSystem: false),
        };
        _sender.Send(Arg.Any<ListProfilesQuery>(), Arg.Any<CancellationToken>())
            .Returns(profiles);

        var result = await _controller.Index(CancellationToken.None);

        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeOfType<ProfileListViewModel>().Subject;
        model.Profiles.Should().HaveCount(2);
    }

    [Fact]
    public async Task Details_WhenProfileExists_ReturnsViewWithDetailViewModel()
    {
        var profileId = Guid.NewGuid();
        var profile = new GetProfileResult(profileId, "Auditor", "Desc", IsSystem: false, []);
        var permissions = new List<ListPermissionsGroupResult>();

        _sender.Send(Arg.Any<GetProfileQuery>(), Arg.Any<CancellationToken>())
            .Returns(profile);
        _sender.Send(Arg.Any<ListPermissionsQuery>(), Arg.Any<CancellationToken>())
            .Returns(permissions);

        var result = await _controller.Details(profileId, CancellationToken.None);

        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeOfType<ProfileDetailViewModel>().Subject;
        model.Profile.Id.Should().Be(profileId);
    }

    [Fact]
    public async Task Details_WhenProfileNotFound_ReturnsNotFound()
    {
        _sender.Send(Arg.Any<GetProfileQuery>(), Arg.Any<CancellationToken>())
            .Returns((GetProfileResult?)null);

        var result = await _controller.Details(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public void Create_Get_ReturnsViewWithEmptyFormModel()
    {
        var result = _controller.Create();

        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        viewResult.Model.Should().BeOfType<CreateProfileFormModel>();
    }

    [Fact]
    public async Task Create_Post_WhenValid_SendsCommandAndRedirects()
    {
        var form = new CreateProfileFormModel("New Profile", "Description");
        _sender.Send(Arg.Any<CreateProfileCommand>(), Arg.Any<CancellationToken>())
            .Returns(new CreateProfileResult(Guid.NewGuid(), form.Name, form.Description));

        var result = await _controller.Create(form, CancellationToken.None);

        await _sender.Received(1).Send(
            Arg.Is<CreateProfileCommand>(c => c.Name == form.Name && c.Description == form.Description),
            Arg.Any<CancellationToken>());
        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be("Index");
    }

    [Fact]
    public async Task Create_Post_WhenConflict_ReturnsViewWithError()
    {
        var form = new CreateProfileFormModel("Duplicate", "Desc");
        _sender.Send(Arg.Any<CreateProfileCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new AuthorizationConflictException("Profile already exists."));

        var result = await _controller.Create(form, CancellationToken.None);

        result.Should().BeOfType<ViewResult>();
        _controller.ModelState.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Edit_Get_WhenSystemProfile_ReturnsForbid()
    {
        var profileId = Guid.NewGuid();
        var profile = new GetProfileResult(profileId, "Administrator", "Sys", IsSystem: true, []);
        _sender.Send(Arg.Any<GetProfileQuery>(), Arg.Any<CancellationToken>())
            .Returns(profile);

        var result = await _controller.Edit(profileId, CancellationToken.None);

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task Edit_Get_WhenNonSystemProfile_ReturnsViewWithFormModel()
    {
        var profileId = Guid.NewGuid();
        var profile = new GetProfileResult(profileId, "Auditor", "Desc", IsSystem: false, []);
        _sender.Send(Arg.Any<GetProfileQuery>(), Arg.Any<CancellationToken>())
            .Returns(profile);

        var result = await _controller.Edit(profileId, CancellationToken.None);

        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeOfType<EditProfileFormModel>().Subject;
        model.Id.Should().Be(profileId);
    }

    [Fact]
    public async Task Edit_Post_WhenValid_SendsCommandAndRedirects()
    {
        var form = new EditProfileFormModel(Guid.NewGuid(), "Updated", "Updated desc");
        _sender.Send(Arg.Any<UpdateProfileCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var result = await _controller.Edit(form, CancellationToken.None);

        await _sender.Received(1).Send(
            Arg.Is<UpdateProfileCommand>(c => c.Id == form.Id && c.Name == form.Name),
            Arg.Any<CancellationToken>());
        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be("Index");
    }

    [Fact]
    public async Task Delete_Post_SendsCommandAndRedirects()
    {
        var profileId = Guid.NewGuid();
        _sender.Send(Arg.Any<DeleteProfileCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var result = await _controller.Delete(profileId, CancellationToken.None);

        await _sender.Received(1).Send(
            Arg.Is<DeleteProfileCommand>(c => c.Id == profileId),
            Arg.Any<CancellationToken>());
        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be("Index");
    }

    [Fact]
    public async Task Delete_Post_WhenForbidden_SetsTempDataErrorAndRedirects()
    {
        _sender.Send(Arg.Any<DeleteProfileCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new AuthorizationForbiddenException("System profile cannot be deleted."));

        var result = await _controller.Delete(Guid.NewGuid(), CancellationToken.None);

        _tempData.Should().ContainKey("Error");
        _tempData["Error"].Should().BeOfType<string>().Which.Should().NotBeNullOrEmpty();
        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be("Index");
    }

    [Fact]
    public async Task SetPermissions_Post_SendsCommandAndRedirectsToDetails()
    {
        var profileId = Guid.NewGuid();
        var selectedIds = new List<Guid> { Guid.NewGuid() };
        _sender.Send(Arg.Any<SetProfilePermissionsCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var result = await _controller.SetPermissions(profileId, selectedIds, CancellationToken.None);

        await _sender.Received(1).Send(
            Arg.Is<SetProfilePermissionsCommand>(c => c.ProfileId == profileId),
            Arg.Any<CancellationToken>());
        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.ActionName.Should().Be("Details");
    }
}
