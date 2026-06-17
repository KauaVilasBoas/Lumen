using System.Net;
using AegisIdentity.DataAccess.Persistence;
using AegisIdentity.Domain.Authorization;
using AegisIdentity.Domain.Users;
using AegisIdentity.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AegisIdentity.IntegrationTests.Authorization;

[Collection(IntegrationCollection.Name)]
[Trait("Category", "Integration")]
public sealed class UserRestoreEndpointTests
{
    private const string BaseEndpoint = "/api/users";

    private readonly IntegrationFixture _fixture;

    public UserRestoreEndpointTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Restore_AnonymousRequest_Returns401()
    {
        var client = _fixture.CreateAnonymousClient();

        var response = await client.PostAsync($"{BaseEndpoint}/{Guid.NewGuid()}/restore", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Restore_AuthenticatedWithoutPermission_Returns403()
    {
        var client = _fixture.CreateAuthenticatedClient("92000000-0000-0000-0000-000000000001");

        var response = await client.PostAsync($"{BaseEndpoint}/{Guid.NewGuid()}/restore", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Restore_WhenUserNotFound_Returns404()
    {
        const string actorUserId = "92000000-0000-0000-0000-000000000002";

        await using var scope = _fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AegisIdentityDbContext>();
        var permissionCache = scope.ServiceProvider.GetRequiredService<IUserPermissionCache>();
        await AuthorizationSeeder.SeedUserWithPermissionAsync(db, permissionCache, Guid.Parse(actorUserId), "Users.Restore");

        var client = _fixture.CreateAuthenticatedClient(actorUserId);
        var response = await client.PostAsync($"{BaseEndpoint}/{Guid.NewGuid()}/restore", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Restore_WhenUserIsNotDeleted_Returns404()
    {
        const string actorUserId = "92000000-0000-0000-0000-000000000003";

        await using var scope = _fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AegisIdentityDbContext>();
        var permissionCache = scope.ServiceProvider.GetRequiredService<IUserPermissionCache>();
        await AuthorizationSeeder.SeedUserWithPermissionAsync(db, permissionCache, Guid.Parse(actorUserId), "Users.Restore");

        var activeUser = User.Create(
            $"restore-active-{Guid.NewGuid():N}@test.com",
            $"restore-active-{Guid.NewGuid():N}",
            "hash");
        db.Users.Add(activeUser);
        await db.SaveChangesAsync();

        var client = _fixture.CreateAuthenticatedClient(actorUserId);
        var response = await client.PostAsync($"{BaseEndpoint}/{activeUser.Id}/restore", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Restore_WhenUserIsDeleted_Returns204()
    {
        const string actorUserId = "92000000-0000-0000-0000-000000000004";

        await using var scope = _fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AegisIdentityDbContext>();
        var permissionCache = scope.ServiceProvider.GetRequiredService<IUserPermissionCache>();
        await AuthorizationSeeder.SeedUserWithPermissionAsync(db, permissionCache, Guid.Parse(actorUserId), "Users.Restore");

        var deletedUser = User.Create(
            $"restore-target-{Guid.NewGuid():N}@test.com",
            $"restore-target-{Guid.NewGuid():N}",
            "hash");
        deletedUser.SoftDelete();
        db.Users.Add(deletedUser);
        await db.SaveChangesAsync();

        var client = _fixture.CreateAuthenticatedClient(actorUserId);
        var response = await client.PostAsync($"{BaseEndpoint}/{deletedUser.Id}/restore", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Restore_WhenUserIsDeleted_RestoresInDatabase()
    {
        const string actorUserId = "92000000-0000-0000-0000-000000000005";

        await using var scope = _fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AegisIdentityDbContext>();
        var permissionCache = scope.ServiceProvider.GetRequiredService<IUserPermissionCache>();
        await AuthorizationSeeder.SeedUserWithPermissionAsync(db, permissionCache, Guid.Parse(actorUserId), "Users.Restore");

        var deletedUser = User.Create(
            $"restore-persist-{Guid.NewGuid():N}@test.com",
            $"restore-persist-{Guid.NewGuid():N}",
            "hash");
        deletedUser.SoftDelete();
        db.Users.Add(deletedUser);
        await db.SaveChangesAsync();

        var client = _fixture.CreateAuthenticatedClient(actorUserId);
        await client.PostAsync($"{BaseEndpoint}/{deletedUser.Id}/restore", null);

        await using var verifyScope = _fixture.Services.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AegisIdentityDbContext>();
        var restoredUser = await verifyDb.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == deletedUser.Id);

        restoredUser.Should().NotBeNull();
        restoredUser!.IsDeleted.Should().BeFalse();
        restoredUser.DeletedAt.Should().BeNull();
    }

    [Fact]
    public async Task Restore_WhenRestoreWindowExpired_Returns409()
    {
        const string actorUserId = "92000000-0000-0000-0000-000000000006";

        await using var scope = _fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AegisIdentityDbContext>();
        var permissionCache = scope.ServiceProvider.GetRequiredService<IUserPermissionCache>();
        await AuthorizationSeeder.SeedUserWithPermissionAsync(db, permissionCache, Guid.Parse(actorUserId), "Users.Restore");

        var expiredUser = User.Create(
            $"restore-expired-{Guid.NewGuid():N}@test.com",
            $"restore-expired-{Guid.NewGuid():N}",
            "hash");
        expiredUser.SoftDelete();
        db.Users.Add(expiredUser);
        await db.SaveChangesAsync();

        typeof(User)
            .GetProperty(nameof(User.DeletedAt))!
            .SetValue(expiredUser, DateTime.UtcNow.AddDays(-31));
        db.Users.Update(expiredUser);
        await db.SaveChangesAsync();

        var client = _fixture.CreateAuthenticatedClient(actorUserId);
        var response = await client.PostAsync($"{BaseEndpoint}/{expiredUser.Id}/restore", null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
