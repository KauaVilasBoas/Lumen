using Lumen.Modules.Identity.Domain.Authorization;
using Lumen.Modules.Identity.Domain.Users;
using Lumen.Modules.Identity.Persistence;
using Lumen.SharedKernel.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace Lumen.IntegrationTests.Infrastructure;

internal static class AuthorizationSeeder
{
    internal static async Task<User> EnsureUserAsync(IdentityDbContext db, Guid userId)
    {
        var existing = await db.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (existing is not null)
            return existing;

        var user = User.Create($"seed-{userId:N}@test.local", $"seed-{userId:N}", "seed-password-hash");
        db.Users.Add(user);
        db.Entry(user).Property(u => u.Id).CurrentValue = userId;
        await db.SaveChangesAsync();

        return user;
    }

    internal static async Task EnsurePermissionAsync(IdentityDbContext db, string permissionCode)
    {
        if (await db.Permissions.AnyAsync(p => p.Code == permissionCode))
            return;

        var parts = permissionCode.Split('.');
        db.Permissions.Add(Permission.Create(parts[0], parts[1], permissionCode));
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Seeds a user with the given permission and invalidates the Redis permission cache
    /// so the next request re-fetches from the database.
    /// </summary>
    internal static async Task SeedUserWithPermissionAsync(
        IdentityDbContext db,
        IDistributedCache distributedCache,
        Guid userId,
        string permissionCode)
    {
        await EnsureUserAsync(db, userId);
        await EnsurePermissionAsync(db, permissionCode);

        var permission = await db.Permissions.FirstAsync(p => p.Code == permissionCode);

        var profileName = $"test-profile-{userId:N}";
        var profile = await db.Profiles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Name == profileName);

        if (profile is null)
        {
            profile = Profile.Create(profileName, profileName);
            db.Profiles.Add(profile);
            await db.SaveChangesAsync();
        }

        if (!await db.PermissionProfiles.AnyAsync(pp => pp.ProfileId == profile.Id && pp.PermissionId == permission.Id))
        {
            db.PermissionProfiles.Add(PermissionProfile.Create(permission.Id, profile.Id));
            await db.SaveChangesAsync();
        }

        if (!await db.UserProfiles.AnyAsync(up => up.UserId == userId && up.ProfileId == profile.Id))
        {
            db.UserProfiles.Add(UserProfile.Create(userId, profile.Id));
            await db.SaveChangesAsync();
        }

        await distributedCache.RemoveAsync(CacheKeys.UserPermissions(userId));
    }
}
