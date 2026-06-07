using System.Net;
using AegisIdentity.DataAccess.Persistence;
using AegisIdentity.Domain.Authorization;
using AegisIdentity.IntegrationTests.Infrastructure;
using AegisIdentity.SharedKernel.Constants;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AegisIdentity.IntegrationTests.Authorization;

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

        await using var scope = _fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AegisIdentityDbContext>();
        await SeedUserWithPermissionAsync(db, Guid.Parse(userId), PermissionCodes.AuthorizationGraph.View);

        var client = _fixture.CreateAuthenticatedClient(userId);

        var response = await client.GetAsync(Endpoint);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static async Task SeedUserWithPermissionAsync(
        AegisIdentityDbContext db,
        Guid userId,
        string permissionCode)
    {
        if (!db.Permissions.Any(p => p.Code == permissionCode))
        {
            var parts = permissionCode.Split('.');
            db.Permissions.Add(Permission.Create(parts[0], parts[1], permissionCode));
            await db.SaveChangesAsync();
        }

        var permission = db.Permissions.First(p => p.Code == permissionCode);

        var profileName = $"test-authgraph-profile-{userId}";
        var profile = db.Profiles
            .IgnoreQueryFilters()
            .FirstOrDefault(p => p.Name == profileName);

        if (profile is null)
        {
            profile = Profile.Create(profileName, profileName);
            db.Profiles.Add(profile);
            await db.SaveChangesAsync();
        }

        if (!db.PermissionProfiles.Any(pp => pp.ProfileId == profile.Id && pp.PermissionId == permission.Id))
        {
            db.PermissionProfiles.Add(PermissionProfile.Create(permission.Id, profile.Id));
            await db.SaveChangesAsync();
        }

        if (!db.UserProfiles.Any(up => up.UserId == userId && up.ProfileId == profile.Id))
        {
            db.UserProfiles.Add(UserProfile.Create(userId, profile.Id));
            await db.SaveChangesAsync();
        }
    }
}
