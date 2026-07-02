using Lumen.Modularity;

namespace Lumen.Authorization.Contracts.Events;

public sealed record UserPermissionsChangedEvent(Guid UserId) : IntegrationEvent;
