using Lumen.Api.Authorization;
using Lumen.Domain.Authorization;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Lumen.UnitTests.Authorization;

public sealed class PermissionSyncServiceTests
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IGroupPermissionRepository _groupPermissionRepository;
    private readonly PermissionSyncService _sut;

    public PermissionSyncServiceTests()
    {
        _permissionRepository = Substitute.For<IPermissionRepository>();
        _groupPermissionRepository = Substitute.For<IGroupPermissionRepository>();

        _sut = new PermissionSyncService(
            _permissionRepository,
            _groupPermissionRepository,
            NullLogger<PermissionSyncService>.Instance);
    }

    [Fact]
    public async Task SyncAsync_WithNewPermission_InsertsIt()
    {
        _permissionRepository.ListAllAsync().Returns([]);
        _groupPermissionRepository.FindByNameAsync("Users").Returns((GroupPermission?)null);
        _groupPermissionRepository
            .When(r => r.InsertAsync(Arg.Any<GroupPermission>()))
            .Do(_ => { });

        var discovered = new List<DiscoveredPermission>
        {
            new("Users", "Delete", "Users.Delete", "Users — Delete", "Users"),
        };

        await _sut.SyncAsync(discovered);

        await _permissionRepository.Received(1).InsertAsync(
            Arg.Is<Permission>(p => p.Code == "Users.Delete"));
    }

    [Fact]
    public async Task SyncAsync_WithExistingPermission_UpdatesItWithoutDuplicate()
    {
        var existingPermission = Permission.Create("Users", "Delete", "Users — Delete");

        _permissionRepository.ListAllAsync().Returns([existingPermission]);
        _groupPermissionRepository.FindByNameAsync("Users").Returns((GroupPermission?)null);
        _groupPermissionRepository
            .When(r => r.InsertAsync(Arg.Any<GroupPermission>()))
            .Do(_ => { });

        var discovered = new List<DiscoveredPermission>
        {
            new("Users", "Delete", "Users.Delete", "Users — Delete", "Users"),
        };

        await _sut.SyncAsync(discovered);

        await _permissionRepository.DidNotReceive().InsertAsync(Arg.Any<Permission>());
        await _permissionRepository.Received(1).UpdateAsync(existingPermission);
    }

    [Fact]
    public async Task SyncAsync_WithOrphanedPermission_MarksItAndDoesNotDelete()
    {
        var orphanCandidate = Permission.Create("Orders", "Archive", "Orders — Archive");

        _permissionRepository.ListAllAsync().Returns([orphanCandidate]);
        _groupPermissionRepository.FindByNameAsync(Arg.Any<string>()).Returns((GroupPermission?)null);

        await _sut.SyncAsync([]);

        orphanCandidate.IsOrphan.Should().BeTrue();
        orphanCandidate.OrphanedAt.Should().NotBeNull();

        await _permissionRepository.Received(1).SaveAllAsync(
            Arg.Is<IEnumerable<Permission>>(list => list.Contains(orphanCandidate)));

        await _permissionRepository.DidNotReceive().InsertAsync(Arg.Any<Permission>());
    }

    [Fact]
    public async Task SyncAsync_WhenOrphanIsRediscovered_ClearsOrphanFlag()
    {
        var orphaned = Permission.Create("Users", "Delete", "Users — Delete");
        orphaned.MarkAsOrphan();

        _permissionRepository.ListAllAsync().Returns([orphaned]);
        _groupPermissionRepository.FindByNameAsync("Users").Returns((GroupPermission?)null);
        _groupPermissionRepository
            .When(r => r.InsertAsync(Arg.Any<GroupPermission>()))
            .Do(_ => { });

        var discovered = new List<DiscoveredPermission>
        {
            new("Users", "Delete", "Users.Delete", "Users — Delete", "Users"),
        };

        await _sut.SyncAsync(discovered);

        orphaned.IsOrphan.Should().BeFalse();
        orphaned.OrphanedAt.Should().BeNull();

        await _permissionRepository.Received(1).UpdateAsync(orphaned);
    }

    [Fact]
    public async Task SyncAsync_AlreadyOrphanedPermissionNotInDiscovered_IsNotMarkedAgain()
    {
        var alreadyOrphaned = Permission.Create("Legacy", "OldAction", "Legacy — OldAction");
        alreadyOrphaned.MarkAsOrphan();
        var originalOrphanedAt = alreadyOrphaned.OrphanedAt;

        _permissionRepository.ListAllAsync().Returns([alreadyOrphaned]);
        _groupPermissionRepository.FindByNameAsync(Arg.Any<string>()).Returns((GroupPermission?)null);

        await _sut.SyncAsync([]);

        await _permissionRepository.DidNotReceive().SaveAllAsync(Arg.Any<IEnumerable<Permission>>());

        alreadyOrphaned.OrphanedAt.Should().Be(originalOrphanedAt);
    }

    [Fact]
    public async Task SyncAsync_ReusesExistingGroup_WhenGroupAlreadyExists()
    {
        var existingGroup = GroupPermission.Create("Users", "Users");

        _permissionRepository.ListAllAsync().Returns([]);
        _groupPermissionRepository.FindByNameAsync("Users").Returns(existingGroup);

        var discovered = new List<DiscoveredPermission>
        {
            new("Users", "Delete", "Users.Delete", "Users — Delete", "Users"),
        };

        await _sut.SyncAsync(discovered);

        await _groupPermissionRepository.DidNotReceive().InsertAsync(Arg.Any<GroupPermission>());
        await _permissionRepository.Received(1).InsertAsync(
            Arg.Is<Permission>(p => p.GroupPermissionId == existingGroup.Id));
    }

    [Fact]
    public async Task SyncAsync_WithEmptyDiscovered_AndNoExisting_DoesNothing()
    {
        _permissionRepository.ListAllAsync().Returns([]);

        await _sut.SyncAsync([]);

        await _permissionRepository.DidNotReceive().InsertAsync(Arg.Any<Permission>());
        await _permissionRepository.DidNotReceive().UpdateAsync(Arg.Any<Permission>());
        await _permissionRepository.DidNotReceive().SaveAllAsync(Arg.Any<IEnumerable<Permission>>());
    }
}
