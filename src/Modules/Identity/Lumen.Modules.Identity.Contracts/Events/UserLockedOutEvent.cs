using Lumen.Modularity;

namespace Lumen.Modules.Identity.Contracts.Events;

public sealed record UserLockedOutEvent(Guid UserId, string Username) : IntegrationEvent;
