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
public sealed class UsersEndpointTests
{
    private const string Endpoint = "/api/users";

    private readonly IntegrationFixture _fixture;

    public UsersEndpointTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Get_AnonymousRequest_Returns401()
    {
        var client = _fixture.CreateAnonymousClient();

        var response = await client.GetAsync(Endpoint);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_AuthenticatedWithoutPermission_Returns403()
    {
        var client = _fixture.CreateAuthenticatedClient("99000000-0000-0000-0000-000000000001");

        var response = await client.GetAsync(Endpoint);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Get_AuthenticatedWithUsersListPermission_Returns200()
    {
        const string userId = "99000000-0000-0000-0000-000000000002";

        await using var db = _fixture.CreateIdentityDbContext();
        await using var scope = _fixture.Services.CreateAsyncScope();
        var permissionCache = scope.ServiceProvider.GetRequiredService<IDistributedCache>();
        await AuthorizationSeeder.SeedUserWithPermissionAsync(db, permissionCache, Guid.Parse(userId), PermissionCodes.Users.List);

        var client = _fixture.CreateAuthenticatedClient(userId);

        var response = await client.GetAsync(Endpoint);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_Returns200_WithExpectedPagedShape()
    {
        const string userId = "99000000-0000-0000-0000-000000000003";

        await using var db = _fixture.CreateIdentityDbContext();
        await using var scope = _fixture.Services.CreateAsyncScope();
        var permissionCache = scope.ServiceProvider.GetRequiredService<IDistributedCache>();
        await AuthorizationSeeder.SeedUserWithPermissionAsync(db, permissionCache, Guid.Parse(userId), PermissionCodes.Users.List);

        var client = _fixture.CreateAuthenticatedClient(userId);
        var response = await client.GetAsync(Endpoint);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.TryGetProperty("items", out _).Should().BeTrue("response must contain 'items'");
        json.TryGetProperty("page", out _).Should().BeTrue("response must contain 'page'");
        json.TryGetProperty("pageSize", out _).Should().BeTrue("response must contain 'pageSize'");
        json.TryGetProperty("total", out _).Should().BeTrue("response must contain 'total'");
    }

    [Theory]
    [InlineData("?page=0")]
    [InlineData("?pageSize=0")]
    [InlineData("?pageSize=101")]
    public async Task Get_WithInvalidPagingParameters_Returns400(string queryString)
    {
        const string userId = "99000000-0000-0000-0000-000000000004";

        await using var db = _fixture.CreateIdentityDbContext();
        await using var scope = _fixture.Services.CreateAsyncScope();
        var permissionCache = scope.ServiceProvider.GetRequiredService<IDistributedCache>();
        await AuthorizationSeeder.SeedUserWithPermissionAsync(db, permissionCache, Guid.Parse(userId), PermissionCodes.Users.List);

        var client = _fixture.CreateAuthenticatedClient(userId);
        var response = await client.GetAsync(Endpoint + queryString);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Get_WithInvalidStateValue_Returns400()
    {
        const string userId = "99000000-0000-0000-0000-000000000005";

        await using var db = _fixture.CreateIdentityDbContext();
        await using var scope = _fixture.Services.CreateAsyncScope();
        var permissionCache = scope.ServiceProvider.GetRequiredService<IDistributedCache>();
        await AuthorizationSeeder.SeedUserWithPermissionAsync(db, permissionCache, Guid.Parse(userId), PermissionCodes.Users.List);

        var client = _fixture.CreateAuthenticatedClient(userId);
        var response = await client.GetAsync(Endpoint + "?state=invalid_value");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Get_WithStateAll_IncludesDeletedUsers()
    {
        const string requestingUserId = "99000000-0000-0000-0000-000000000006";

        await using var db = _fixture.CreateIdentityDbContext();
        await using var scope = _fixture.Services.CreateAsyncScope();
        var permissionCache = scope.ServiceProvider.GetRequiredService<IDistributedCache>();
        await AuthorizationSeeder.SeedUserWithPermissionAsync(db, permissionCache, Guid.Parse(requestingUserId), PermissionCodes.Users.List);

        var deletedUser = User.Create(
            $"deleted-{Guid.NewGuid():N}@test.com",
            $"deleted-{Guid.NewGuid():N}",
            "hash");
        deletedUser.SoftDelete();
        db.Users.Add(deletedUser);
        await db.SaveChangesAsync();

        var client = _fixture.CreateAuthenticatedClient(requestingUserId);
        var response = await client.GetAsync(Endpoint + "?state=all");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var items = json.GetProperty("items");

        items.EnumerateArray()
             .Any(item => item.GetProperty("state").GetString() == "deleted")
             .Should().BeTrue("state=all must include deleted users");
    }

    [Fact]
    public async Task Get_WithStateActive_HidesDeletedUsers()
    {
        const string requestingUserId = "99000000-0000-0000-0000-000000000007";

        await using var db = _fixture.CreateIdentityDbContext();
        await using var scope = _fixture.Services.CreateAsyncScope();
        var permissionCache = scope.ServiceProvider.GetRequiredService<IDistributedCache>();
        await AuthorizationSeeder.SeedUserWithPermissionAsync(db, permissionCache, Guid.Parse(requestingUserId), PermissionCodes.Users.List);

        var marker = $"shouldbehidden-{Guid.NewGuid():N}";
        var deletedUser = User.Create($"{marker}@test.com", marker, "hash");
        deletedUser.SoftDelete();
        db.Users.Add(deletedUser);
        await db.SaveChangesAsync();

        var client = _fixture.CreateAuthenticatedClient(requestingUserId);
        var response = await client.GetAsync(Endpoint + "?state=active");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var items = json.GetProperty("items");

        items.EnumerateArray()
             .Any(item =>
             {
                 var email = item.GetProperty("email").GetString() ?? string.Empty;
                 return email.Contains(marker);
             })
             .Should().BeFalse("state=active must not include soft-deleted users");
    }
}
