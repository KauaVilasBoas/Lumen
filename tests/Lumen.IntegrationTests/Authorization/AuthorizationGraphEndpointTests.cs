using Microsoft.Extensions.Caching.Distributed;
using System.Net;
using FluentAssertions;
using Lumen.IntegrationTests.Infrastructure;

using Lumen.SharedKernel.Constants;
using Microsoft.Extensions.DependencyInjection;

namespace Lumen.IntegrationTests.Authorization;

[Collection(IntegrationCollection.Name)]
[Trait("Category", "Integration")]
public sealed class AuthorizationGraphEndpointTests
{
    private const string Endpoint = "/api/authorization-graph";

    private readonly IntegrationFixture _fixture;

    public AuthorizationGraphEndpointTests(IntegrationFixture fixture)
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
        var client = _fixture.CreateAuthenticatedClient("88000000-0000-0000-0000-000000000001");

        var response = await client.GetAsync(Endpoint);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Get_AuthenticatedWithPermission_Returns200()
    {
        const string userId = "88000000-0000-0000-0000-000000000002";

        await using var db = _fixture.CreateIdentityDbContext();
        await using var scope = _fixture.Services.CreateAsyncScope();
        var permissionCache = scope.ServiceProvider.GetRequiredService<IDistributedCache>();
        await AuthorizationSeeder.SeedUserWithPermissionAsync(db, permissionCache, Guid.Parse(userId), PermissionCodes.AuthorizationGraph.View);

        var client = _fixture.CreateAuthenticatedClient(userId);

        var response = await client.GetAsync(Endpoint);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
