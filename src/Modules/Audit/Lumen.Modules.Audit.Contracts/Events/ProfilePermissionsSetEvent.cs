using Lumen.Modularity;

namespace Lumen.Modules.Audit.Contracts.Events;

public sealed record ProfilePermissionsSetEvent(
    Guid ProfileId,
    string ProfileName,
    string ActorUsername) : IntegrationEvent;
