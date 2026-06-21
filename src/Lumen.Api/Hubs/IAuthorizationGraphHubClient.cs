using Lumen.ReadModels.Queries;

namespace Lumen.Api.Hubs;

public interface IAuthorizationGraphHubClient
{
    Task GraphUpdated(GetAuthorizationGraphQueryHandler.GraphSnapshot delta);

    Task UserPermissionsInvalidated(Guid userId);
}
