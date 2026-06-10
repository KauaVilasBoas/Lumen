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
        await AuthorizationSeeder.SeedUserWithPermissionAsync(db, Guid.Parse(userId), PermissionCodes.AuthorizationGraph.View);

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

}
