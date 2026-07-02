using System.Security.Claims;
using Lumen.Api.Hubs;
using Lumen.Authorization.Contracts;
using Lumen.SharedKernel.Constants;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using NSubstitute;

namespace Lumen.UnitTests.Authorization;

public sealed class AuthorizationGraphHubTests
{
    private readonly IUserPermissionService _permissionService;
    private readonly HubCallerContext _context;
    private readonly AuthorizationGraphHub _sut;

    public AuthorizationGraphHubTests()
    {
        _permissionService = Substitute.For<IUserPermissionService>();
        _context = Substitute.For<HubCallerContext>();

        _sut = new AuthorizationGraphHub(_permissionService);
        _sut.Context = _context;
    }

    [Fact]
    public async Task OnConnectedAsync_WhenUserHasPermission_DoesNotAbortConnection()
    {
        var userId = Guid.NewGuid();
        SetupCallerWithSubject(userId.ToString());
        _permissionService
            .HasPermissionAsync(userId, PermissionCodes.AuthorizationGraph.View, Arg.Any<CancellationToken>())
            .Returns(true);

        await _sut.OnConnectedAsync();

        _context.DidNotReceive().Abort();
    }

    [Fact]
    public async Task OnConnectedAsync_WhenUserLacksPermission_AbortsConnection()
    {
        var userId = Guid.NewGuid();
        SetupCallerWithSubject(userId.ToString());
        _permissionService
            .HasPermissionAsync(userId, PermissionCodes.AuthorizationGraph.View, Arg.Any<CancellationToken>())
            .Returns(false);

        await _sut.OnConnectedAsync();

        _context.Received(1).Abort();
    }

    [Fact]
    public async Task OnConnectedAsync_WhenSubjectClaimIsMissing_AbortsConnection()
    {
        _context.User.Returns(new ClaimsPrincipal(new ClaimsIdentity()));

        await _sut.OnConnectedAsync();

        _context.Received(1).Abort();
        await _permissionService.DidNotReceive()
            .HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OnConnectedAsync_WhenSubjectClaimIsNotAGuid_AbortsConnection()
    {
        SetupCallerWithSubject("not-a-guid");

        await _sut.OnConnectedAsync();

        _context.Received(1).Abort();
        await _permissionService.DidNotReceive()
            .HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    private void SetupCallerWithSubject(string subject)
    {
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, subject)
        ]);

        _context.User.Returns(new ClaimsPrincipal(identity));
    }
}
