using Microsoft.Extensions.Caching.Distributed;
using FluentAssertions;
using Lumen.IntegrationTests.Infrastructure;
using Lumen.Modules.Identity.Domain.Authorization;

using Lumen.SharedKernel.Constants;
using Lumen.SharedKernel.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lumen.IntegrationTests.Authorization;

[Collection(IntegrationCollection.Name)]
[Trait("Category", "Integration")]
public sealed class ProfileManagementTests
{
    private readonly IntegrationFixture _fixture;

    public ProfileManagementTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CreateProfile_WithUniqueName_PersistsToDatabase()
    {
        await using var db = _fixture.CreateIdentityDbContext();

        var profile = Profile.Create("Managers", "Profile for managers");
        db.Profiles.Add(profile);
        await db.SaveChangesAsync();

        var persisted = await db.Profiles.FirstOrDefaultAsync(p => p.Id == profile.Id);

        persisted.Should().NotBeNull();
        persisted!.Name.Should().Be("Managers");
        persisted.IsSystem.Should().BeFalse();
        persisted.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task SoftDeleteProfile_SetsIsDeletedAndCascadesToJoinRows()
    {
        await using var db = _fixture.CreateIdentityDbContext();

        var permission = Permission.Create("Reports", "View", "Reports — View");
        db.Permissions.Add(permission);

        var profile = Profile.Create("Analysts", "Profile for analysts");
        db.Profiles.Add(profile);

        var userId = Guid.NewGuid();
        await AuthorizationSeeder.EnsureUserAsync(db, userId);
        var userProfile = UserProfile.Create(userId, profile.Id);
        db.UserProfiles.Add(userProfile);

        var permissionProfile = PermissionProfile.Create(permission.Id, profile.Id);
        db.PermissionProfiles.Add(permissionProfile);

        await db.SaveChangesAsync();

        permissionProfile.SoftDelete();
        db.PermissionProfiles.Update(permissionProfile);

        userProfile.SoftDelete();
        db.UserProfiles.Update(userProfile);

        profile.SoftDelete();
        db.Profiles.Update(profile);

        await db.SaveChangesAsync();

        var deletedProfile = await db.Profiles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == profile.Id);

        deletedProfile.Should().NotBeNull();
        deletedProfile!.IsDeleted.Should().BeTrue();
        deletedProfile.DeletedAt.Should().NotBeNull();

        var deletedUserProfile = await db.UserProfiles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(up => up.Id == userProfile.Id);

        deletedUserProfile!.IsDeleted.Should().BeTrue();

        var deletedPermissionProfile = await db.PermissionProfiles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(pp => pp.Id == permissionProfile.Id);

        deletedPermissionProfile!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task SoftDeleteProfile_SystemProfile_ThrowsForbiddenException()
    {
        await using var db = _fixture.CreateIdentityDbContext();

        var adminProfile = await db.Profiles
            .FirstOrDefaultAsync(p => p.Id == SystemProfiles.AdministratorId);

        adminProfile.Should().NotBeNull();

        var act = () => adminProfile!.SoftDelete();

        act.Should().Throw<ForbiddenException>()
            .WithMessage(BackofficeErrorMessages.SystemProfileCannotBeDeleted);
    }

    [Fact]
    public async Task SetProfilePermissions_SoftDeletesRemovedAndAddsNew()
    {
        await using var db = _fixture.CreateIdentityDbContext();

        var permA = Permission.Create("Invoices", "List", "Invoices — List");
        var permB = Permission.Create("Invoices", "Export", "Invoices — Export");
        db.Permissions.AddRange(permA, permB);

        var profile = Profile.Create("Finance", "Finance profile");
        db.Profiles.Add(profile);

        var ppA = PermissionProfile.Create(permA.Id, profile.Id);
        db.PermissionProfiles.Add(ppA);

        await db.SaveChangesAsync();

        ppA.SoftDelete();
        db.PermissionProfiles.Update(ppA);

        var ppB = PermissionProfile.Create(permB.Id, profile.Id);
        db.PermissionProfiles.Add(ppB);

        await db.SaveChangesAsync();

        var activeAssignments = await db.PermissionProfiles
            .Where(pp => pp.ProfileId == profile.Id)
            .ToListAsync();

        activeAssignments.Should().HaveCount(1);
        activeAssignments[0].PermissionId.Should().Be(permB.Id);

        var softDeletedAssignment = await db.PermissionProfiles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(pp => pp.Id == ppA.Id);

        softDeletedAssignment!.IsDeleted.Should().BeTrue("removed permission must be soft-deleted, never physically removed");
    }

    [Fact]
    public async Task AssignUserProfile_CreatesActiveJoinRow()
    {
        await using var db = _fixture.CreateIdentityDbContext();

        var profile = Profile.Create("Support", "Support team profile");
        db.Profiles.Add(profile);
        await db.SaveChangesAsync();

        var userId = Guid.NewGuid();
        await AuthorizationSeeder.EnsureUserAsync(db, userId);
        var userProfile = UserProfile.Create(userId, profile.Id);
        db.UserProfiles.Add(userProfile);
        await db.SaveChangesAsync();

        var persisted = await db.UserProfiles
            .FirstOrDefaultAsync(up => up.UserId == userId && up.ProfileId == profile.Id);

        persisted.Should().NotBeNull();
        persisted!.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task RemoveUserProfile_SoftDeletesJoinRow_NotPhysicalDelete()
    {
        await using var db = _fixture.CreateIdentityDbContext();

        var profile = Profile.Create("Auditors", "Auditors profile");
        db.Profiles.Add(profile);
        await db.SaveChangesAsync();

        var userId = Guid.NewGuid();
        await AuthorizationSeeder.EnsureUserAsync(db, userId);
        var userProfile = UserProfile.Create(userId, profile.Id);
        db.UserProfiles.Add(userProfile);
        await db.SaveChangesAsync();

        userProfile.SoftDelete();
        db.UserProfiles.Update(userProfile);
        await db.SaveChangesAsync();

        var activeRow = await db.UserProfiles
            .FirstOrDefaultAsync(up => up.UserId == userId && up.ProfileId == profile.Id);

        activeRow.Should().BeNull("soft-deleted row must be filtered out by the global query filter");

        var rawRow = await db.UserProfiles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(up => up.Id == userProfile.Id);

        rawRow.Should().NotBeNull("row must still exist in the database — soft-delete only");
        rawRow!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task CacheInvalidation_AfterSetProfilePermissions_ReflectsNewPermissionsImmediately()
    {
        const string userId = "00000000-0000-0000-0000-000000000010";
        var userGuid = Guid.Parse(userId);

        await using var scope = _fixture.Services.CreateAsyncScope();
        await using var db = _fixture.CreateIdentityDbContext();
        var cache = scope.ServiceProvider.GetRequiredService<IDistributedCache>();

        var permission = Permission.Create("Documents", "Read", "Documents — Read");
        db.Permissions.Add(permission);

        var profile = Profile.Create("Readers", "Readers profile");
        db.Profiles.Add(profile);

        await AuthorizationSeeder.EnsureUserAsync(db, userGuid);
        db.UserProfiles.Add(UserProfile.Create(userGuid, profile.Id));
        await db.SaveChangesAsync();

        var cacheKey = CacheKeys.UserPermissions(userGuid);
        var beforePermissions = await cache.GetStringAsync(cacheKey);
        beforePermissions.Should().BeNull("cache should start empty");

        var permissionProfile = PermissionProfile.Create(permission.Id, profile.Id);
        db.PermissionProfiles.Add(permissionProfile);
        await db.SaveChangesAsync();

        await cache.RemoveAsync(cacheKey);

        var afterPermissions = await cache.GetStringAsync(cacheKey);
        afterPermissions.Should().BeNull("cache was explicitly invalidated; next read will rebuild from DB");
    }

    [Fact]
    public async Task ProfileNameUniqueness_AllowsDuplicateNamesOnSoftDeletedProfiles()
    {
        await using var db = _fixture.CreateIdentityDbContext();

        var profile1 = Profile.Create("TempProfile", "First temp");
        db.Profiles.Add(profile1);
        await db.SaveChangesAsync();

        profile1.SoftDelete();
        db.Profiles.Update(profile1);
        await db.SaveChangesAsync();

        var profile2 = Profile.Create("TempProfile", "Second temp");
        db.Profiles.Add(profile2);
        var act = async () => await db.SaveChangesAsync();

        await act.Should().NotThrowAsync(
            "the unique index is partial ([IsDeleted] = 0), so a deleted profile does not block re-use of the name");
    }

    [Fact]
    public async Task DeleteWithCascade_SoftDeletesProfileAndAllAssociationsAtomically()
    {
        await using var scope = _fixture.Services.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IProfileRepository>();
        await using var db = _fixture.CreateIdentityDbContext();

        var permission = Permission.Create("Items", "Delete", "Items — Delete");
        db.Permissions.Add(permission);

        var profile = Profile.Create("Deletable", "Will be cascade-deleted");
        db.Profiles.Add(profile);

        var userId = Guid.NewGuid();
        await AuthorizationSeeder.EnsureUserAsync(db, userId);
        var userProfile = UserProfile.Create(userId, profile.Id);
        db.UserProfiles.Add(userProfile);

        var permissionProfile = PermissionProfile.Create(permission.Id, profile.Id);
        db.PermissionProfiles.Add(permissionProfile);

        await db.SaveChangesAsync();

        permissionProfile.SoftDelete();
        userProfile.SoftDelete();
        profile.SoftDelete();

        await repository.DeleteWithCascadeAsync(
            profile,
            new List<PermissionProfile> { permissionProfile },
            new List<UserProfile> { userProfile });

        var rawProfile = await db.Profiles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == profile.Id);
        rawProfile.Should().NotBeNull();
        rawProfile!.IsDeleted.Should().BeTrue();
        rawProfile.DeletedAt.Should().NotBeNull();

        var rawUserProfile = await db.UserProfiles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(up => up.Id == userProfile.Id);
        rawUserProfile.Should().NotBeNull();
        rawUserProfile!.IsDeleted.Should().BeTrue();

        var rawPermissionProfile = await db.PermissionProfiles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(pp => pp.Id == permissionProfile.Id);
        rawPermissionProfile.Should().NotBeNull();
        rawPermissionProfile!.IsDeleted.Should().BeTrue();

        var activeProfile = await db.Profiles.FirstOrDefaultAsync(p => p.Id == profile.Id);
        activeProfile.Should().BeNull("soft-deleted profile must be hidden by the global query filter");
    }

    [Fact]
    public async Task DeleteWithCascade_WithNoAssociations_SoftDeletesOnlyProfile()
    {
        await using var scope = _fixture.Services.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IProfileRepository>();
        await using var db = _fixture.CreateIdentityDbContext();

        var profile = Profile.Create("IsolatedProfile", "No associations");
        db.Profiles.Add(profile);
        await db.SaveChangesAsync();

        profile.SoftDelete();

        await repository.DeleteWithCascadeAsync(
            profile,
            Array.Empty<PermissionProfile>(),
            Array.Empty<UserProfile>());

        var rawProfile = await db.Profiles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == profile.Id);

        rawProfile.Should().NotBeNull();
        rawProfile!.IsDeleted.Should().BeTrue();
    }
}
