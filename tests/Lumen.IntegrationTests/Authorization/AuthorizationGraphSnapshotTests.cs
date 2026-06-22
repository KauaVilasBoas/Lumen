using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Lumen.DataAccess.Persistence;
using Lumen.Domain.Authorization;
using Lumen.IntegrationTests.Infrastructure;
using Lumen.SharedKernel.Constants;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lumen.IntegrationTests.Authorization;

[Collection(IntegrationCollection.Name)]
[Trait("Category", "Integration")]
public sealed class AuthorizationGraphSnapshotTests
{
    private const string Endpoint = "/api/authorization-graph";

    private readonly IntegrationFixture _fixture;

    public AuthorizationGraphSnapshotTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Authentication / authorisation enforcement
    // ──────────────────────────────────────────────────────────────────────────

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
        var client = _fixture.CreateAuthenticatedClient("86000000-0000-0000-0000-000000000001");

        var response = await client.GetAsync(Endpoint);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Get_AuthenticatedWithPermission_Returns200()
    {
        const string userId = "86000000-0000-0000-0000-000000000002";

        await using var scope = _fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LumenDbContext>();
        var permissionCache = scope.ServiceProvider.GetRequiredService<IUserPermissionCache>();
        await AuthorizationSeeder.SeedUserWithPermissionAsync(db, permissionCache, Guid.Parse(userId), PermissionCodes.AuthorizationGraph.View);

        var client = _fixture.CreateAuthenticatedClient(userId);

        var response = await client.GetAsync(Endpoint);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Response shape
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Get_WithPermission_ReturnsGraphSnapshotShape()
    {
        const string userId = "86000000-0000-0000-0000-000000000003";

        await using var scope = _fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LumenDbContext>();
        var permissionCache = scope.ServiceProvider.GetRequiredService<IUserPermissionCache>();
        await AuthorizationSeeder.SeedUserWithPermissionAsync(db, permissionCache, Guid.Parse(userId), PermissionCodes.AuthorizationGraph.View);

        var client = _fixture.CreateAuthenticatedClient(userId);

        var response = await client.GetAsync(Endpoint);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.TryGetProperty("users", out _).Should().BeTrue("response must contain 'users'");
        json.TryGetProperty("profiles", out _).Should().BeTrue("response must contain 'profiles'");
        json.TryGetProperty("permissions", out _).Should().BeTrue("response must contain 'permissions'");
    }

    [Fact]
    public async Task Get_WithPermission_UsersArrayItemsHaveExpectedFields()
    {
        const string userId = "86000000-0000-0000-0000-000000000004";

        await using var scope = _fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LumenDbContext>();
        var permissionCache = scope.ServiceProvider.GetRequiredService<IUserPermissionCache>();
        await AuthorizationSeeder.SeedUserWithPermissionAsync(db, permissionCache, Guid.Parse(userId), PermissionCodes.AuthorizationGraph.View);

        var client = _fixture.CreateAuthenticatedClient(userId);

        var response = await client.GetAsync(Endpoint);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        var users = json.GetProperty("users");
        users.ValueKind.Should().Be(JsonValueKind.Array);

        if (users.GetArrayLength() > 0)
        {
            var first = users[0];
            first.TryGetProperty("id", out _).Should().BeTrue();
            first.TryGetProperty("username", out _).Should().BeTrue();
            first.TryGetProperty("email", out _).Should().BeTrue();
            first.TryGetProperty("state", out _).Should().BeTrue();
            first.TryGetProperty("profiles", out _).Should().BeTrue();
        }
    }

    [Fact]
    public async Task Get_WithPermission_ResponseDoesNotContainColorField()
    {
        const string userId = "86000000-0000-0000-0000-000000000005";

        await using var scope = _fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LumenDbContext>();
        var permissionCache = scope.ServiceProvider.GetRequiredService<IUserPermissionCache>();
        await AuthorizationSeeder.SeedUserWithPermissionAsync(db, permissionCache, Guid.Parse(userId), PermissionCodes.AuthorizationGraph.View);

        var client = _fixture.CreateAuthenticatedClient(userId);

        var responseBody = await (await client.GetAsync(Endpoint)).Content.ReadAsStringAsync();

        responseBody.Should().NotContain("\"color\"",
            "color is a presentation concern and must not appear in the API payload");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────────

}
