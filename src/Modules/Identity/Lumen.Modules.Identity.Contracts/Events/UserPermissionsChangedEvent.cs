using Lumen.Modularity;

namespace Lumen.Modules.Identity.Contracts.Events;

public sealed record UserPermissionsChangedEvent(Guid UserId) : IntegrationEvent;
