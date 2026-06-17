using System.Net;
using AegisIdentity.DataAccess.Persistence;
using AegisIdentity.Domain.Authorization;
using AegisIdentity.Domain.Users;
using AegisIdentity.IntegrationTests.Infrastructure;
using AegisIdentity.SharedKernel.Constants;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AegisIdentity.IntegrationTests.Authorization;

[Collection(IntegrationCollection.Name)]
[Trait("Category", "Integration")]
public sealed class UserDeleteEndpointTests
{
    private const string BaseEndpoint = "/api/users";

    private readonly IntegrationFixture _fixture;

    public UserDeleteEndpointTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Delete_AnonymousRequest_Returns401()
    {
        var client = _fixture.CreateAnonymousClient();

        var response = await client.DeleteAsync($"{BaseEndpoint}/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Delete_AuthenticatedWithoutPermission_Returns403()
    {
        var client = _fixture.CreateAuthenticatedClient("91000000-0000-0000-0000-000000000001");

        var response = await client.DeleteAsync($"{BaseEndpoint}/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Delete_WhenUserNotFound_Returns404()
    {
        const string actorUserId = "91000000-0000-0000-0000-000000000002";

        await using var scope = _fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AegisIdentityDbContext>();
        var permissionCache = scope.ServiceProvider.GetRequiredService<IUserPermissionCache>();
        await AuthorizationSeeder.SeedUserWithPermissionAsync(db, permissionCache, Guid.Parse(actorUserId), "Users.Delete");

        var client = _fixture.CreateAuthenticatedClient(actorUserId);
        var response = await client.DeleteAsync($"{BaseEndpoint}/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_WhenUserExists_Returns204()
    {
        const string actorUserId = "91000000-0000-0000-0000-000000000003";

        await using var scope = _fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AegisIdentityDbContext>();
        var permissionCache = scope.ServiceProvider.GetRequiredService<IUserPermissionCache>();
        await AuthorizationSeeder.SeedUserWithPermissionAsync(db, permissionCache, Guid.Parse(actorUserId), "Users.Delete");

        var targetUser = User.Create(
            $"delete-target-{Guid.NewGuid():N}@test.com",
            $"delete-target-{Guid.NewGuid():N}",
            "hash");
        db.Users.Add(targetUser);
        await db.SaveChangesAsync();

        var client = _fixture.CreateAuthenticatedClient(actorUserId);
        var response = await client.DeleteAsync($"{BaseEndpoint}/{targetUser.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_WhenUserExists_SoftDeletesInDatabase()
    {
        const string actorUserId = "91000000-0000-0000-0000-000000000004";

        await using var scope = _fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AegisIdentityDbContext>();
        var permissionCache = scope.ServiceProvider.GetRequiredService<IUserPermissionCache>();
        await AuthorizationSeeder.SeedUserWithPermissionAsync(db, permissionCache, Guid.Parse(actorUserId), "Users.Delete");

        var targetUser = User.Create(
            $"delete-persist-{Guid.NewGuid():N}@test.com",
            $"delete-persist-{Guid.NewGuid():N}",
            "hash");
        db.Users.Add(targetUser);
        await db.SaveChangesAsync();

        var client = _fixture.CreateAuthenticatedClient(actorUserId);
        await client.DeleteAsync($"{BaseEndpoint}/{targetUser.Id}");

        await using var verifyScope = _fixture.Services.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AegisIdentityDbContext>();
        var deletedUser = await verifyDb.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == targetUser.Id);

        deletedUser.Should().NotBeNull();
        deletedUser!.IsDeleted.Should().BeTrue();
        deletedUser.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Delete_WhenLastAdministrator_Returns409()
    {
        const string actorUserId = "91000000-0000-0000-0000-000000000005";

        await using var scope = _fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AegisIdentityDbContext>();
        var permissionCache = scope.ServiceProvider.GetRequiredService<IUserPermissionCache>();
        await AuthorizationSeeder.SeedUserWithPermissionAsync(db, permissionCache, Guid.Parse(actorUserId), "Users.Delete");

        var adminUserGuid = Guid.Parse(actorUserId);
        if (!await db.UserProfiles.AnyAsync(up => up.UserId == adminUserGuid && up.ProfileId == SystemProfiles.AdministratorId))
        {
            db.UserProfiles.Add(UserProfile.Create(adminUserGuid, SystemProfiles.AdministratorId));
            await db.SaveChangesAsync();
            await permissionCache.InvalidateAsync(adminUserGuid);
        }

        var client = _fixture.CreateAuthenticatedClient(actorUserId);
        var response = await client.DeleteAsync($"{BaseEndpoint}/{actorUserId}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
