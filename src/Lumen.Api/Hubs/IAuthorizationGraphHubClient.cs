using Lumen.Authorization.Application.Queries;

namespace Lumen.Api.Hubs;

public interface IAuthorizationGraphHubClient
{
    Task GraphUpdated(AuthorizationGraphSnapshot delta);

    Task UserPermissionsInvalidated(Guid userId);
}
