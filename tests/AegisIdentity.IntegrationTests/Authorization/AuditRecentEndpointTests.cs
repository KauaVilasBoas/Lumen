using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AegisIdentity.DataAccess.Persistence;
using AegisIdentity.Domain.Audit;
using AegisIdentity.Domain.Authorization;
using AegisIdentity.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AegisIdentity.IntegrationTests.Authorization;

[Collection(IntegrationCollection.Name)]
[Trait("Category", "Integration")]
public sealed class AuditRecentEndpointTests
{
    private const string BaseEndpoint = "/api/audit/recent";

    private readonly IntegrationFixture _fixture;

    public AuditRecentEndpointTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Authentication / authorisation enforcement
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Recent_AnonymousRequest_Returns401()
    {
        var client = _fixture.CreateAnonymousClient();

        var response = await client.GetAsync(BaseEndpoint);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Recent_AuthenticatedWithoutPermission_Returns403()
    {
        var client = _fixture.CreateAuthenticatedClient("97000000-0000-0000-0000-000000000001");

        var response = await client.GetAsync(BaseEndpoint);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Response shape
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Recent_AuthenticatedWithAuditReadPermission_Returns200WithExpectedShape()
    {
        const string requestingUserId = "97000000-0000-0000-0000-000000000002";

        await using var scope = _fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AegisIdentityDbContext>();
        await AuthorizationSeeder.SeedUserWithPermissionAsync(db, Guid.Parse(requestingUserId), "Audit.Read");
        await SeedAuditEntryAsync(db);

        var client = _fixture.CreateAuthenticatedClient(requestingUserId);
        var response = await client.GetAsync(BaseEndpoint);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.ValueKind.Should().Be(JsonValueKind.Array);

        if (json.GetArrayLength() > 0)
        {
            var first = json[0];
            first.TryGetProperty("kind", out _).Should().BeTrue("response items must contain 'kind'");
            first.TryGetProperty("message", out _).Should().BeTrue("response items must contain 'message'");
            first.TryGetProperty("occurredAt", out _).Should().BeTrue("response items must contain 'occurredAt'");
        }
    }

    [Fact]
    public async Task Recent_WithTakeParameter_RespectsLimit()
    {
        const string requestingUserId = "97000000-0000-0000-0000-000000000003";

        await using var scope = _fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AegisIdentityDbContext>();
        await AuthorizationSeeder.SeedUserWithPermissionAsync(db, Guid.Parse(requestingUserId), "Audit.Read");
        await SeedAuditEntryAsync(db);
        await SeedAuditEntryAsync(db);
        await SeedAuditEntryAsync(db);

        var client = _fixture.CreateAuthenticatedClient(requestingUserId);
        var response = await client.GetAsync($"{BaseEndpoint}?take=2");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetArrayLength().Should().BeLessOrEqualTo(2);
    }

    [Fact]
    public async Task Recent_InvalidTake_Returns400()
    {
        const string requestingUserId = "97000000-0000-0000-0000-000000000004";

        await using var scope = _fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AegisIdentityDbContext>();
        await AuthorizationSeeder.SeedUserWithPermissionAsync(db, Guid.Parse(requestingUserId), "Audit.Read");

        var client = _fixture.CreateAuthenticatedClient(requestingUserId);
        var response = await client.GetAsync($"{BaseEndpoint}?take=0");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────────

    private static async Task SeedAuditEntryAsync(AegisIdentityDbContext db)
    {
        db.AuditEntries.Add(AuditEntry.Create(
            kind: "auth.login",
            actor: "seed-user",
            target: null,
            message: "Seed audit entry for integration test."));

        await db.SaveChangesAsync();
    }
}
