using Lumen.Modularity;

namespace Lumen.Modules.Audit.Contracts.Events;

public sealed record UserPermissionsChangedEvent(Guid UserId) : IntegrationEvent;
