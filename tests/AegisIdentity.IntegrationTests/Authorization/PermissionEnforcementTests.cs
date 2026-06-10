using System.Net;
using AegisIdentity.DataAccess.Persistence;
using AegisIdentity.Domain.Authorization;
using AegisIdentity.IntegrationTests.Infrastructure;
using AegisIdentity.SharedKernel.Authorization;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace AegisIdentity.IntegrationTests.Authorization;

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
    public async Task AuthenticatedUser_WithoutRequiredPermission_Returns403()
    {
        await using var scope = _fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AegisIdentityDbContext>();
        await AuthorizationSeeder.EnsurePermissionAsync(db, PermissionProbeController.ProbePermissionCode);

        var client = _fixture.CreateProbeClientWithUser("00000000-0000-0000-0000-000000000005");
        var response = await client.GetAsync(PermissionProbeController.ProtectedPath);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AnonymousUser_OnPermissionProtectedEndpoint_Returns401()
    {
        var client = _fixture.CreateAnonymousProbeClient();
        var response = await client.GetAsync(PermissionProbeController.ProtectedPath);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AuthenticatedUser_WithRequiredPermission_Returns200()
    {
        const string userId = "00000000-0000-0000-0000-000000000002";

        await using var scope = _fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AegisIdentityDbContext>();
        await AuthorizationSeeder.SeedUserWithPermissionAsync(db, Guid.Parse(userId), PermissionProbeController.ProbePermissionCode);

        var client = _fixture.CreateProbeClientWithUser(userId);
        var response = await client.GetAsync(PermissionProbeController.ProtectedPath);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CacheInvalidation_AfterProfileChange_ReflectsNewPermissionsImmediately()
    {
        const string userId = "00000000-0000-0000-0000-000000000003";
        var userGuid = Guid.Parse(userId);

        await using var scope = _fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AegisIdentityDbContext>();
        var cache = scope.ServiceProvider.GetRequiredService<IUserPermissionCache>();

        await AuthorizationSeeder.EnsurePermissionAsync(db, PermissionProbeController.ProbePermissionCode);

        var client = _fixture.CreateProbeClientWithUser(userId);

        var firstResponse = await client.GetAsync(PermissionProbeController.ProtectedPath);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        await AuthorizationSeeder.SeedUserWithPermissionAsync(db, userGuid, PermissionProbeController.ProbePermissionCode);
        await cache.InvalidateAsync(userGuid);

        var secondResponse = await client.GetAsync(PermissionProbeController.ProtectedPath);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AuthenticatedUser_WithPermission_WhenRedisUnavailable_FallsBackToDatabase_Returns200()
    {
        const string userId = "00000000-0000-0000-0000-000000000004";

        await using var scope = _fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AegisIdentityDbContext>();
        await AuthorizationSeeder.SeedUserWithPermissionAsync(db, Guid.Parse(userId), PermissionProbeController.ProbePermissionCode);

        var client = _fixture.CreateProbeClientWithBrokenRedis(userId);
        var response = await client.GetAsync(PermissionProbeController.ProtectedPath);

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "authorization must fall back to the database when Redis is unavailable");
    }

}

[ApiController]
[Route("probe")]
public sealed class PermissionProbeController : ControllerBase
{
    public const string ProbePermissionCode = "Probe.Protected";
    public const string ProtectedPath = "/probe/protected";

    [HttpGet("protected")]
    [RequirePermission(ProbePermissionCode)]
    [Authorize(Policy = ProbePermissionCode)]
    public IActionResult Protected() => Ok(new { ok = true });
}
