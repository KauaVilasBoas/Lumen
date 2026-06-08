using AegisIdentity.DataAccess.Persistence;
using AegisIdentity.Domain.Authorization;
using AegisIdentity.IntegrationTests.Infrastructure;
using AegisIdentity.SharedKernel.Constants;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AegisIdentity.IntegrationTests.Authorization;

[Collection(IntegrationCollection.Name)]
[Trait("Category", "Integration")]
public sealed class AuthorizationGraphHubTests
{
    private static readonly string HubPath = HubRoutes.AuthorizationGraph;

    private readonly IntegrationFixture _fixture;

    public AuthorizationGraphHubTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Connect_WithoutPermission_ConnectionIsAborted()
    {
        var token = _fixture.BuildJwtForUser("77000000-0000-0000-0000-000000000001");
        var connection = BuildHubConnection(token);

        var act = async () => await connection.StartAsync();

        await act.Should().ThrowAsync<Exception>("connection must be refused for user without permission");

        await connection.DisposeAsync();
    }

    [Fact]
    public async Task Connect_WithPermission_ConnectionSucceeds()
    {
        const string userId = "77000000-0000-0000-0000-000000000002";

        await using var scope = _fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AegisIdentityDbContext>();
        await SeedUserWithPermissionAsync(db, Guid.Parse(userId), PermissionCodes.AuthorizationGraph.View);

        var token = _fixture.BuildJwtForUser(userId);
        var connection = BuildHubConnection(token);

        await connection.StartAsync();

        connection.State.Should().Be(HubConnectionState.Connected);

        await connection.StopAsync();
        await connection.DisposeAsync();
    }

    private HubConnection BuildHubConnection(string token)
    {
        var httpHandler = _fixture.Server.CreateHandler();

        return new HubConnectionBuilder()
            .WithUrl($"http://localhost{HubPath}", options =>
            {
                options.HttpMessageHandlerFactory = _ => httpHandler;
                options.AccessTokenProvider = () => Task.FromResult<string?>(token);
            })
            .Build();
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

        var profileName = $"test-hub-profile-{userId}";
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
