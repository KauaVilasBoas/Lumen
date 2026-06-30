using Microsoft.Extensions.Caching.Distributed;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Lumen.IntegrationTests.Infrastructure;
using Lumen.Modules.Audit.Domain;
using Lumen.Modules.Audit.Persistence;

using Lumen.SharedKernel.Constants;
using Microsoft.Extensions.DependencyInjection;

namespace Lumen.IntegrationTests.Authorization;

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

    [Fact]
    public async Task Recent_AuthenticatedWithAuditReadPermission_Returns200WithExpectedShape()
    {
        const string requestingUserId = "97000000-0000-0000-0000-000000000002";

        await using var identityDb = _fixture.CreateIdentityDbContext();
        await using var scope = _fixture.Services.CreateAsyncScope();
        var permissionCache = scope.ServiceProvider.GetRequiredService<IDistributedCache>();
        await AuthorizationSeeder.SeedUserWithPermissionAsync(identityDb, permissionCache, Guid.Parse(requestingUserId), PermissionCodes.Audit.Read);

        await using var auditDb = _fixture.CreateAuditDbContext();
        await SeedAuditEntryAsync(auditDb);

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

        await using var identityDb = _fixture.CreateIdentityDbContext();
        await using var scope = _fixture.Services.CreateAsyncScope();
        var permissionCache = scope.ServiceProvider.GetRequiredService<IDistributedCache>();
        await AuthorizationSeeder.SeedUserWithPermissionAsync(identityDb, permissionCache, Guid.Parse(requestingUserId), PermissionCodes.Audit.Read);

        await using var auditDb = _fixture.CreateAuditDbContext();
        await SeedAuditEntryAsync(auditDb);
        await SeedAuditEntryAsync(auditDb);
        await SeedAuditEntryAsync(auditDb);

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

        await using var identityDb = _fixture.CreateIdentityDbContext();
        await using var scope = _fixture.Services.CreateAsyncScope();
        var permissionCache = scope.ServiceProvider.GetRequiredService<IDistributedCache>();
        await AuthorizationSeeder.SeedUserWithPermissionAsync(identityDb, permissionCache, Guid.Parse(requestingUserId), PermissionCodes.Audit.Read);

        var client = _fixture.CreateAuthenticatedClient(requestingUserId);
        var response = await client.GetAsync($"{BaseEndpoint}?take=0");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static async Task SeedAuditEntryAsync(AuditDbContext db)
    {
        db.AuditEntries.Add(AuditEntry.Create(
            kind: AuditEventKinds.AuthLogin,
            actor: "seed-user",
            target: null,
            message: "Seed audit entry for integration test."));

        await db.SaveChangesAsync();
    }
}
