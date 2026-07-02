using Lumen.Modularity;

namespace Lumen.Authorization.Contracts.Events;

public sealed record ProfilePermissionsSetEvent(
    Guid ProfileId,
    string ProfileName,
    string ActorUsername) : IntegrationEvent;
