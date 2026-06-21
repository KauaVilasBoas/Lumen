using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Lumen.DataAccess.Persistence;
using Lumen.Domain.Authorization;
using Lumen.Domain.Users;
using Lumen.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Lumen.IntegrationTests.Authorization;

[Collection(IntegrationCollection.Name)]
[Trait("Category", "Integration")]
public sealed class UserUpdateEndpointTests
{
    private const string BaseEndpoint = "/api/users";

    private readonly IntegrationFixture _fixture;

    public UserUpdateEndpointTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Authentication / authorisation enforcement
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Put_AnonymousRequest_Returns401()
    {
        var client = _fixture.CreateAnonymousClient();

        var response = await client.PutAsJsonAsync($"{BaseEndpoint}/{Guid.NewGuid()}", new { });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Put_AuthenticatedWithoutPermission_Returns403()
    {
        var client = _fixture.CreateAuthenticatedClient("96000000-0000-0000-0000-000000000001");

        var response = await client.PutAsJsonAsync($"{BaseEndpoint}/{Guid.NewGuid()}", new { });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 404 — user not found
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Put_WithUsersUpdatePermission_NonExistentId_Returns404()
    {
        const string requestingUserId = "96000000-0000-0000-0000-000000000002";

        await using var scope = _fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LumenDbContext>();
        var permissionCache = scope.ServiceProvider.GetRequiredService<IUserPermissionCache>();
        await AuthorizationSeeder.SeedUserWithPermissionAsync(db, permissionCache, Guid.Parse(requestingUserId), "Users.Update");

        var client = _fixture.CreateAuthenticatedClient(requestingUserId);
        var response = await client.PutAsJsonAsync(
            $"{BaseEndpoint}/{Guid.NewGuid()}",
            new { username = "newusername" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Happy path — username update
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Put_ChangeUsername_Returns200WithEmailChangedFalse()
    {
        const string requestingUserId = "96000000-0000-0000-0000-000000000003";
        var targetUserId = Guid.Parse("96000000-0000-0000-0000-000000000004");

        await using var scope = _fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LumenDbContext>();
        var permissionCache = scope.ServiceProvider.GetRequiredService<IUserPermissionCache>();
        await AuthorizationSeeder.SeedUserWithPermissionAsync(db, permissionCache, Guid.Parse(requestingUserId), "Users.Update");
        await AuthorizationSeeder.EnsureUserAsync(db, targetUserId);

        var client = _fixture.CreateAuthenticatedClient(requestingUserId);
        var newUsername = $"updated-{Guid.NewGuid():N}"[..20];
        var response = await client.PutAsJsonAsync(
            $"{BaseEndpoint}/{targetUserId}",
            new { username = newUsername });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("emailChanged").GetBoolean().Should().BeFalse();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Happy path — email update
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Put_ChangeEmail_Returns200WithEmailChangedTrue()
    {
        const string requestingUserId = "96000000-0000-0000-0000-000000000005";
        var targetUserId = Guid.Parse("96000000-0000-0000-0000-000000000006");

        await using var scope = _fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LumenDbContext>();
        var permissionCache = scope.ServiceProvider.GetRequiredService<IUserPermissionCache>();
        await AuthorizationSeeder.SeedUserWithPermissionAsync(db, permissionCache, Guid.Parse(requestingUserId), "Users.Update");
        await AuthorizationSeeder.EnsureUserAsync(db, targetUserId);

        var client = _fixture.CreateAuthenticatedClient(requestingUserId);
        var newEmail = $"updated-{Guid.NewGuid():N}@test.com";
        var response = await client.PutAsJsonAsync(
            $"{BaseEndpoint}/{targetUserId}",
            new { email = newEmail });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("emailChanged").GetBoolean().Should().BeTrue();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 409 — conflict on duplicate
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Put_WithDuplicateUsername_Returns409()
    {
        const string requestingUserId = "96000000-0000-0000-0000-000000000007";
        var targetUserId = Guid.Parse("96000000-0000-0000-0000-000000000008");

        await using var scope = _fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LumenDbContext>();
        var permissionCache = scope.ServiceProvider.GetRequiredService<IUserPermissionCache>();
        await AuthorizationSeeder.SeedUserWithPermissionAsync(db, permissionCache, Guid.Parse(requestingUserId), "Users.Update");
        await AuthorizationSeeder.EnsureUserAsync(db, targetUserId);

        var takenUsername = $"taken-{Guid.NewGuid():N}"[..20];
        var takenUser = User.Create($"{takenUsername}@test.com", takenUsername, "hash");
        db.Users.Add(takenUser);
        await db.SaveChangesAsync();

        var client = _fixture.CreateAuthenticatedClient(requestingUserId);
        var response = await client.PutAsJsonAsync(
            $"{BaseEndpoint}/{targetUserId}",
            new { username = takenUsername });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Put_WithDuplicateEmail_Returns409()
    {
        const string requestingUserId = "96000000-0000-0000-0000-000000000009";
        var targetUserId = Guid.Parse("96000000-0000-0000-0000-000000000010");

        await using var scope = _fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LumenDbContext>();
        var permissionCache = scope.ServiceProvider.GetRequiredService<IUserPermissionCache>();
        await AuthorizationSeeder.SeedUserWithPermissionAsync(db, permissionCache, Guid.Parse(requestingUserId), "Users.Update");
        await AuthorizationSeeder.EnsureUserAsync(db, targetUserId);

        var takenEmail = $"taken-{Guid.NewGuid():N}@test.com";
        var takenUser = User.Create(takenEmail, $"taken-{Guid.NewGuid():N}"[..20], "hash");
        db.Users.Add(takenUser);
        await db.SaveChangesAsync();

        var client = _fixture.CreateAuthenticatedClient(requestingUserId);
        var response = await client.PutAsJsonAsync(
            $"{BaseEndpoint}/{targetUserId}",
            new { email = takenEmail });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 400 — validation failure
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Put_WithInvalidEmail_Returns400()
    {
        const string requestingUserId = "96000000-0000-0000-0000-000000000011";
        var targetUserId = Guid.Parse("96000000-0000-0000-0000-000000000012");

        await using var scope = _fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LumenDbContext>();
        var permissionCache = scope.ServiceProvider.GetRequiredService<IUserPermissionCache>();
        await AuthorizationSeeder.SeedUserWithPermissionAsync(db, permissionCache, Guid.Parse(requestingUserId), "Users.Update");
        await AuthorizationSeeder.EnsureUserAsync(db, targetUserId);

        var client = _fixture.CreateAuthenticatedClient(requestingUserId);
        var response = await client.PutAsJsonAsync(
            $"{BaseEndpoint}/{targetUserId}",
            new { email = "notanemail" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
