using Microsoft.Extensions.Caching.Distributed;
using System.Net;
using FluentAssertions;
using Lumen.IntegrationTests.Infrastructure;

using Lumen.SharedKernel.Constants;
using Microsoft.Extensions.DependencyInjection;

namespace Lumen.IntegrationTests.Authorization;

[Collection(IntegrationCollection.Name)]
[Trait("Category", "Integration")]
public sealed class PermissionEnforcementTests
{
    private readonly IntegrationFixture _fixture;

    public PermissionEnforcementTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AuthenticatedUser_WithoutRequiredPermission_OnUsersEndpoint_Returns403()
    {
        var client = _fixture.CreateAuthenticatedClient("00000000-0000-0000-0000-000000000005");

        var response = await client.GetAsync("/api/users");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AnonymousUser_OnPermissionProtectedEndpoint_Returns401()
    {
        var client = _fixture.CreateAnonymousClient();

        var response = await client.GetAsync("/api/users");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AuthenticatedUser_WithRequiredPermission_Returns200()
    {
        const string userId = "00000000-0000-0000-0000-000000000006";

        await using var db = _fixture.CreateIdentityDbContext();
        await using var scope = _fixture.Services.CreateAsyncScope();
        var permissionCache = scope.ServiceProvider.GetRequiredService<IDistributedCache>();
        await AuthorizationSeeder.SeedUserWithPermissionAsync(db, permissionCache, Guid.Parse(userId), PermissionCodes.Users.List);

        var client = _fixture.CreateAuthenticatedClient(userId);
        var response = await client.GetAsync("/api/users");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CacheInvalidation_AfterPermissionGrant_ReflectsNewPermissionsImmediately()
    {
        const string userId = "00000000-0000-0000-0000-000000000007";
        var userGuid = Guid.Parse(userId);

        await using var db = _fixture.CreateIdentityDbContext();
        await using var scope = _fixture.Services.CreateAsyncScope();
        var cache = scope.ServiceProvider.GetRequiredService<IDistributedCache>();

        await AuthorizationSeeder.EnsureUserAsync(db, userGuid);

        var client = _fixture.CreateAuthenticatedClient(userId);

        var firstResponse = await client.GetAsync("/api/users");
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        await AuthorizationSeeder.SeedUserWithPermissionAsync(db, cache, userGuid, PermissionCodes.Users.List);

        var secondResponse = await client.GetAsync("/api/users");
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AuthenticatedUser_WithPermission_WhenRedisUnavailable_FallsBackToDatabase_Returns200()
    {
        const string userId = "00000000-0000-0000-0000-000000000008";

        await using var db = _fixture.CreateIdentityDbContext();
        await using var scope = _fixture.Services.CreateAsyncScope();
        var permissionCache = scope.ServiceProvider.GetRequiredService<IDistributedCache>();
        await AuthorizationSeeder.SeedUserWithPermissionAsync(db, permissionCache, Guid.Parse(userId), PermissionCodes.Users.List);

        var client = _fixture.CreateClientWithBrokenRedis(userId);
        var response = await client.GetAsync("/api/users");

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "authorization must fall back to the database when Redis is unavailable");
    }
}
