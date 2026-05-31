using AegisIdentity.DataAccess.Persistence;
using AegisIdentity.Domain.Authorization;
using AegisIdentity.IntegrationTests.Infrastructure;
using AegisIdentity.SharedKernel.Constants;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AegisIdentity.IntegrationTests.Authorization;

[Collection(IntegrationCollection.Name)]
[Trait("Category", "Integration")]
public sealed class DefaultProfilesTests
{
    private readonly IntegrationFixture _fixture;

    public DefaultProfilesTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SeedDefaultProfiles_Migration_CreatesAdministratorProfile()
    {
        await using var db = _fixture.CreateDbContext();

        var profile = await db.Profiles
            .FirstOrDefaultAsync(p => p.Id == SystemProfiles.AdministratorId);

        profile.Should().NotBeNull();
        profile!.Name.Should().Be("Administrator");
        profile.IsSystem.Should().BeTrue();
        profile.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task SeedDefaultProfiles_Migration_CreatesUserProfile()
    {
        await using var db = _fixture.CreateDbContext();

        var profile = await db.Profiles
            .FirstOrDefaultAsync(p => p.Id == SystemProfiles.UserId);

        profile.Should().NotBeNull();
        profile!.Name.Should().Be("User");
        profile.IsSystem.Should().BeTrue();
        profile.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task SeedDefaultProfiles_Migration_BindsAdminUserToAdministratorProfile()
    {
        await using var db = _fixture.CreateDbContext();

        var userProfile = await db.UserProfiles
            .FirstOrDefaultAsync(up =>
                up.UserId == SystemUsers.AdminId &&
                up.ProfileId == SystemProfiles.AdministratorId);

        userProfile.Should().NotBeNull();
        userProfile!.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task AdministratorPermissionReconciliation_AfterDiscovery_AdminHoldsAllPermissions()
    {
        await using var scope = _fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AegisIdentityDbContext>();

        var allPermissionIds = await db.Permissions
            .Where(p => !p.IsDeleted)
            .Select(p => p.Id)
            .ToListAsync();

        if (allPermissionIds.Count == 0)
            return;

        var assignedPermissionIds = await db.PermissionProfiles
            .Where(pp => pp.ProfileId == SystemProfiles.AdministratorId && !pp.IsDeleted)
            .Select(pp => pp.PermissionId)
            .ToListAsync();

        assignedPermissionIds.Should().Contain(allPermissionIds,
            "Administrator profile must hold every discovered permission after reconciliation");
    }

    [Fact]
    public async Task AdministratorPermissionReconciliation_IsAdditive_DoesNotRemoveExistingAssignments()
    {
        await using var scope = _fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AegisIdentityDbContext>();

        var existingAssignmentIds = await db.PermissionProfiles
            .Where(pp => pp.ProfileId == SystemProfiles.AdministratorId && !pp.IsDeleted)
            .Select(pp => pp.Id)
            .ToListAsync();

        var extraPermission = Permission.Create("Test", "AdditiveCheck", "Test.AdditiveCheck");
        db.Permissions.Add(extraPermission);
        await db.SaveChangesAsync();

        var reconciliationService = scope.ServiceProvider
            .GetRequiredService<AegisIdentity.Api.Authorization.AdministratorPermissionReconciliationService>();

        await reconciliationService.ReconcileAsync();

        var updatedAssignmentIds = await db.PermissionProfiles
            .Where(pp => pp.ProfileId == SystemProfiles.AdministratorId && !pp.IsDeleted)
            .Select(pp => pp.Id)
            .ToListAsync();

        updatedAssignmentIds.Should().Contain(existingAssignmentIds,
            "reconciliation must be additive and never remove pre-existing assignments");

        updatedAssignmentIds.Should().Contain(
            updatedAssignmentIds.Except(existingAssignmentIds),
            "the new permission must have been granted");
    }

    [Fact]
    public async Task UserProfile_HasNoPermissionsAssigned_ByDefault()
    {
        await using var db = _fixture.CreateDbContext();

        var userProfilePermissions = await db.PermissionProfiles
            .Where(pp => pp.ProfileId == SystemProfiles.UserId && !pp.IsDeleted)
            .ToListAsync();

        userProfilePermissions.Should().BeEmpty(
            "the User system profile starts with no permissions; they are granted explicitly");
    }
}
