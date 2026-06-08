using AegisIdentity.ReadModels.Queries;

namespace AegisIdentity.Api.Hubs;

public interface IAuthorizationGraphHubClient
{
    Task GraphUpdated(GetAuthorizationGraphQueryHandler.GraphSnapshot delta);

    Task UserPermissionsInvalidated(Guid userId);
}
