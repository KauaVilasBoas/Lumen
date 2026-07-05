using FluentAssertions;
using Lumen.Authorization.Application.Queries;
using Lumen.Authorization.Application.UserProfiles.Assign;
using Lumen.Authorization.Application.UserProfiles.Remove;
using Lumen.Authorization.Backoffice.Areas.Lumen.Controllers;
using Lumen.Authorization.Backoffice.ViewModels;
using Lumen.Authorization.Contracts;
using Lumen.Authorization.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Lumen.Authorization.Backoffice.Tests.Controllers;

public sealed class UsersControllerTests
{
    private readonly ISender _sender;
    private readonly IAuthorizationUserSource _userSource;
    private readonly UsersController _controller;
    private readonly FakeTempData _tempData;

    public UsersControllerTests()
    {
        _sender = Substitute.For<ISender>();
        _userSource = Substitute.For<IAuthorizationUserSource>();
        _tempData = new FakeTempData();
        _controller = new UsersController(_sender, _userSource);
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

    private static AuthorizationUserDto BuildUser(string username = "alice") =>
        new(Guid.NewGuid(), username, $"{username}@example.com", "Active");

    private static List<ListUserProfilesResult> BuildAssigned(int count = 0)
    {
        var list = new List<ListUserProfilesResult>();
        for (var i = 0; i < count; i++)
            list.Add(new ListUserProfilesResult(Guid.NewGuid(), Guid.NewGuid(), $"Profile{i}", IsSystem: false));
        return list;
    }

    private static List<ListProfilesResult> BuildProfiles(int count = 1)
    {
        var list = new List<ListProfilesResult>();
        for (var i = 0; i < count; i++)
            list.Add(new ListProfilesResult(Guid.NewGuid(), $"Profile{i}", "Desc", IsSystem: false));
        return list;
    }

    [Fact]
    public async Task Index_WithUsers_ReturnsViewWithUserListViewModel()
    {
        var user = BuildUser();
        _userSource.ListActiveUsersAsync(Arg.Any<CancellationToken>())
            .Returns(new List<AuthorizationUserDto> { user });
        _sender.Send(Arg.Any<ListUserProfilesQuery>(), Arg.Any<CancellationToken>())
            .Returns(BuildAssigned(2));

        var result = await _controller.Index(CancellationToken.None);

        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeOfType<UserListViewModel>().Subject;
        model.Users.Should().HaveCount(1);
        model.Users[0].ProfileCount.Should().Be(2);
        model.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public async Task Index_WhenUserSourceReturnsEmpty_ReturnsViewWithEmptyState()
    {
        _userSource.ListActiveUsersAsync(Arg.Any<CancellationToken>())
            .Returns(new List<AuthorizationUserDto>());

        var result = await _controller.Index(CancellationToken.None);

        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeOfType<UserListViewModel>().Subject;
        model.IsEmpty.Should().BeTrue();
        model.Users.Should().BeEmpty();
    }

    [Fact]
    public async Task Index_SendsListUserProfilesQueryForEachUser()
    {
        var users = new List<AuthorizationUserDto> { BuildUser("alice"), BuildUser("bob") };
        _userSource.ListActiveUsersAsync(Arg.Any<CancellationToken>())
            .Returns(users);
        _sender.Send(Arg.Any<ListUserProfilesQuery>(), Arg.Any<CancellationToken>())
            .Returns(BuildAssigned());

        await _controller.Index(CancellationToken.None);

        await _sender.Received(2).Send(Arg.Any<ListUserProfilesQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Details_WhenUserExists_ReturnsViewWithAssignmentViewModel()
    {
        var user = BuildUser();
        _userSource.ListActiveUsersAsync(Arg.Any<CancellationToken>())
            .Returns(new List<AuthorizationUserDto> { user });
        _sender.Send(Arg.Any<ListUserProfilesQuery>(), Arg.Any<CancellationToken>())
            .Returns(BuildAssigned(1));
        _sender.Send(Arg.Any<ListProfilesQuery>(), Arg.Any<CancellationToken>())
            .Returns(BuildProfiles(3));

        var result = await _controller.Details(user.Id, CancellationToken.None);

        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeOfType<UserProfileAssignmentViewModel>().Subject;
        model.UserId.Should().Be(user.Id);
        model.Username.Should().Be(user.Username);
        model.AssignedProfiles.Should().HaveCount(1);
        model.AvailableProfiles.Should().HaveCount(3);
    }

    [Fact]
    public async Task Details_WhenUserNotFound_ReturnsNotFound()
    {
        _userSource.ListActiveUsersAsync(Arg.Any<CancellationToken>())
            .Returns(new List<AuthorizationUserDto>());

        var result = await _controller.Details(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Assign_WhenSucceeds_SendsCommandAndRedirectsToDetails()
    {
        var userId = Guid.NewGuid();
        var profileId = Guid.NewGuid();
        _sender.Send(Arg.Any<AssignUserProfileCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var result = await _controller.Assign(userId, profileId, CancellationToken.None);

        await _sender.Received(1).Send(
            Arg.Is<AssignUserProfileCommand>(c => c.UserId == userId && c.ProfileId == profileId),
            Arg.Any<CancellationToken>());
        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.ActionName.Should().Be("Details");
        redirect.RouteValues!["id"].Should().Be(userId);
    }

    [Fact]
    public async Task Assign_WhenProfileNotFound_SetsTempDataErrorAndRedirects()
    {
        var userId = Guid.NewGuid();
        _sender.Send(Arg.Any<AssignUserProfileCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new AuthorizationNotFoundException("Profile not found."));

        var result = await _controller.Assign(userId, Guid.NewGuid(), CancellationToken.None);

        _tempData.Should().ContainKey("Error");
        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be("Details");
    }

    [Fact]
    public async Task Remove_WhenSucceeds_SendsCommandAndRedirectsToDetails()
    {
        var userId = Guid.NewGuid();
        var profileId = Guid.NewGuid();
        _sender.Send(Arg.Any<RemoveUserProfileCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var result = await _controller.Remove(userId, profileId, CancellationToken.None);

        await _sender.Received(1).Send(
            Arg.Is<RemoveUserProfileCommand>(c => c.UserId == userId && c.ProfileId == profileId),
            Arg.Any<CancellationToken>());
        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.ActionName.Should().Be("Details");
        redirect.RouteValues!["id"].Should().Be(userId);
    }

    [Fact]
    public async Task Remove_WhenAssignmentNotFound_SetsTempDataErrorAndRedirects()
    {
        var userId = Guid.NewGuid();
        _sender.Send(Arg.Any<RemoveUserProfileCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new AuthorizationNotFoundException("Assignment not found."));

        var result = await _controller.Remove(userId, Guid.NewGuid(), CancellationToken.None);

        _tempData.Should().ContainKey("Error");
        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be("Details");
    }

    [Fact]
    public async Task Remove_WhenUnexpectedError_SetsTempDataFallbackMessageAndRedirects()
    {
        _sender.Send(Arg.Any<RemoveUserProfileCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Unexpected."));

        var result = await _controller.Remove(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        _tempData.Should().ContainKey("Error");
        _tempData["Error"].Should().BeOfType<string>().Which.Should().NotBeNullOrEmpty();
        result.Should().BeOfType<RedirectToActionResult>();
    }
}
