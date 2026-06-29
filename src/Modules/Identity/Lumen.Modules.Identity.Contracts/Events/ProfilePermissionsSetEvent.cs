using Lumen.Modularity;

namespace Lumen.Modules.Identity.Contracts.Events;

public sealed record ProfilePermissionsSetEvent(
    Guid ProfileId,
    string ProfileName,
    string ActorUsername) : IntegrationEvent;
