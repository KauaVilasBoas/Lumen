using Lumen.Modularity;
using Lumen.Modules.Identity.Application.Queries;
using Lumen.Modules.Identity.Contracts.Events;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace Lumen.Api.Hubs;

internal sealed class GraphLivePushHandler : IIntegrationEventHandler<UserPermissionsChangedEvent>
{
    private readonly IHubContext<AuthorizationGraphHub, IAuthorizationGraphHubClient> _hub;
    private readonly IMediator _mediator;
    private readonly ILogger<GraphLivePushHandler> _logger;

    public GraphLivePushHandler(
        IHubContext<AuthorizationGraphHub, IAuthorizationGraphHubClient> hub,
        IMediator mediator,
        ILogger<GraphLivePushHandler> logger)
    {
        _hub = hub;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task HandleAsync(UserPermissionsChangedEvent @event, CancellationToken cancellationToken = default)
    {
        var query = new GetAuthorizationGraphQuery();
        var snapshot = await _mediator.Send(query, cancellationToken);

        var userNode = snapshot.Users.FirstOrDefault(u => u.Id == @event.UserId);

        if (userNode is null)
        {
            _logger.LogWarning(
                "GraphLivePushHandler: user {UserId} not found in snapshot — skipping hub push.",
                @event.UserId);
            return;
        }

        var profileKeys = new HashSet<string>(userNode.Profiles);
        var userProfiles = snapshot.Profiles
            .Where(kvp => profileKeys.Contains(kvp.Key))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        var permissionKeys = userProfiles.Values
            .SelectMany(p => p.Permissions)
            .ToHashSet();

        var userPermissions = snapshot.Permissions
            .Where(kvp => permissionKeys.Contains(kvp.Key))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        var delta = new AuthorizationGraphSnapshot(
            Users: [userNode],
            Profiles: userProfiles,
            Permissions: userPermissions);

        await _hub.Clients
            .User(@event.UserId.ToString())
            .GraphUpdated(delta);

        _logger.LogInformation(
            "GraphUpdated pushed to hub for user {UserId} with {ProfileCount} profile(s).",
            @event.UserId,
            userNode.Profiles.Count);
    }
}
