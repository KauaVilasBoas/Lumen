using FluentAssertions;
using Lumen.Authorization.Application.Queries;
using Lumen.Authorization.Backoffice.Areas.Lumen.Controllers;
using Lumen.Authorization.Backoffice.ViewModels;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Lumen.Authorization.Backoffice.Tests.Controllers;

public sealed class PermissionsControllerTests
{
    private readonly ISender _sender;
    private readonly PermissionsController _controller;

    public PermissionsControllerTests()
    {
        _sender = Substitute.For<ISender>();
        _controller = new PermissionsController(_sender);
    }

    [Fact]
    public async Task Index_ReturnsViewWithPermissionCatalogueViewModel()
    {
        var groups = new List<ListPermissionsGroupResult>
        {
            new(Guid.NewGuid(), "Users", new List<ListPermissionsPermissionResult>
            {
                new(Guid.NewGuid(), "Users.List", "Users — List", IsOrphan: false),
                new(Guid.NewGuid(), "Users.Get", "Users — Get", IsOrphan: false),
            }),
        };
        _sender.Send(Arg.Any<ListPermissionsQuery>(), Arg.Any<CancellationToken>())
            .Returns(groups);

        var result = await _controller.Index(CancellationToken.None);

        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeOfType<PermissionCatalogueViewModel>().Subject;
        model.Groups.Should().HaveCount(1);
        model.Groups[0].GroupName.Should().Be("Users");
    }

    [Fact]
    public async Task Index_SendsListPermissionsQuery()
    {
        _sender.Send(Arg.Any<ListPermissionsQuery>(), Arg.Any<CancellationToken>())
            .Returns(new List<ListPermissionsGroupResult>());

        await _controller.Index(CancellationToken.None);

        await _sender.Received(1).Send(
            Arg.Any<ListPermissionsQuery>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Index_WhenNoGroups_ReturnsViewWithEmptyModel()
    {
        _sender.Send(Arg.Any<ListPermissionsQuery>(), Arg.Any<CancellationToken>())
            .Returns(new List<ListPermissionsGroupResult>());

        var result = await _controller.Index(CancellationToken.None);

        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeOfType<PermissionCatalogueViewModel>().Subject;
        model.Groups.Should().BeEmpty();
    }
}
