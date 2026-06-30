using Microsoft.Extensions.Caching.Distributed;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Lumen.IntegrationTests.Infrastructure;
using Lumen.Modules.Identity.Domain.Users;

using Lumen.SharedKernel.Constants;
using Microsoft.Extensions.DependencyInjection;

namespace Lumen.IntegrationTests.Authorization;

[Collection(IntegrationCollection.Name)]
[Trait("Category", "Integration")]
public sealed class UserDetailEndpointTests
{
    private const string BaseEndpoint = "/api/users";

    private readonly IntegrationFixture _fixture;

    public UserDetailEndpointTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetDetail_AnonymousRequest_Returns401()
    {
        var client = _fixture.CreateAnonymousClient();

        var response = await client.GetAsync($"{BaseEndpoint}/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetDetail_AuthenticatedWithoutPermission_Returns403()
    {
        var client = _fixture.CreateAuthenticatedClient("98000000-0000-0000-0000-000000000001");

        var response = await client.GetAsync($"{BaseEndpoint}/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetDetail_AuthenticatedWithUsersGetPermission_NonExistentId_Returns404()
    {
        const string requestingUserId = "98000000-0000-0000-0000-000000000002";

        await using var db = _fixture.CreateIdentityDbContext();
        await using var scope = _fixture.Services.CreateAsyncScope();
        var permissionCache = scope.ServiceProvider.GetRequiredService<IDistributedCache>();
        await AuthorizationSeeder.SeedUserWithPermissionAsync(db, permissionCache, Guid.Parse(requestingUserId), PermissionCodes.Users.Get);

        var client = _fixture.CreateAuthenticatedClient(requestingUserId);
        var response = await client.GetAsync($"{BaseEndpoint}/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetDetail_ExistingUser_Returns200WithExpectedShape()
    {
        const string requestingUserId = "98000000-0000-0000-0000-000000000003";

        await using var db = _fixture.CreateIdentityDbContext();
        await using var scope = _fixture.Services.CreateAsyncScope();
        var permissionCache = scope.ServiceProvider.GetRequiredService<IDistributedCache>();
        await AuthorizationSeeder.SeedUserWithPermissionAsync(db, permissionCache, Guid.Parse(requestingUserId), PermissionCodes.Users.Get);

        var targetUser = User.Create(
            $"detail-target-{Guid.NewGuid():N}@test.com",
            $"detail-tgt-{Guid.NewGuid():N}"[..20],
            "hash");
        targetUser.ConfirmEmail();
        db.Users.Add(targetUser);
        await db.SaveChangesAsync();

        var client = _fixture.CreateAuthenticatedClient(requestingUserId);
        var response = await client.GetAsync($"{BaseEndpoint}/{targetUser.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.TryGetProperty("id", out _).Should().BeTrue("response must contain 'id'");
        json.TryGetProperty("username", out _).Should().BeTrue("response must contain 'username'");
        json.TryGetProperty("email", out _).Should().BeTrue("response must contain 'email'");
        json.TryGetProperty("state", out _).Should().BeTrue("response must contain 'state'");
        json.TryGetProperty("isBootstrap", out _).Should().BeTrue("response must contain 'isBootstrap'");
        json.TryGetProperty("createdAt", out _).Should().BeTrue("response must contain 'createdAt'");
        json.TryGetProperty("profiles", out _).Should().BeTrue("response must contain 'profiles'");
        json.TryGetProperty("resolvedPermissionCount", out _).Should().BeTrue("response must contain 'resolvedPermissionCount'");
    }

    [Fact]
    public async Task GetDetail_ActiveUser_ReturnsStateActive()
    {
        const string requestingUserId = "98000000-0000-0000-0000-000000000004";

        await using var db = _fixture.CreateIdentityDbContext();
        await using var scope = _fixture.Services.CreateAsyncScope();
        var permissionCache = scope.ServiceProvider.GetRequiredService<IDistributedCache>();
        await AuthorizationSeeder.SeedUserWithPermissionAsync(db, permissionCache, Guid.Parse(requestingUserId), PermissionCodes.Users.Get);

        var targetUser = User.Create(
            $"active-{Guid.NewGuid():N}@test.com",
            $"active-usr-{Guid.NewGuid():N}"[..20],
            "hash");
        targetUser.ConfirmEmail();
        db.Users.Add(targetUser);
        await db.SaveChangesAsync();

        var client = _fixture.CreateAuthenticatedClient(requestingUserId);
        var response = await client.GetAsync($"{BaseEndpoint}/{targetUser.Id}");
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        json.GetProperty("state").GetString().Should().Be("active");
    }

    [Fact]
    public async Task GetDetail_SoftDeletedUser_Returns200WithDeletedState()
    {
        const string requestingUserId = "98000000-0000-0000-0000-000000000005";

        await using var db = _fixture.CreateIdentityDbContext();
        await using var scope = _fixture.Services.CreateAsyncScope();
        var permissionCache = scope.ServiceProvider.GetRequiredService<IDistributedCache>();
        await AuthorizationSeeder.SeedUserWithPermissionAsync(db, permissionCache, Guid.Parse(requestingUserId), PermissionCodes.Users.Get);

        var deletedUser = User.Create(
            $"deleted-detail-{Guid.NewGuid():N}@test.com",
            $"del-usr-{Guid.NewGuid():N}"[..20],
            "hash");
        deletedUser.SoftDelete();
        db.Users.Add(deletedUser);
        await db.SaveChangesAsync();

        var client = _fixture.CreateAuthenticatedClient(requestingUserId);
        var response = await client.GetAsync($"{BaseEndpoint}/{deletedUser.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("state").GetString().Should().Be("deleted");
    }
}
