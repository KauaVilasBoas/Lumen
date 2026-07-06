using Lumen.Modularity;

namespace Lumen.Identity.Contracts.Events;

public sealed record UserLockedOutEvent(Guid UserId, string Username) : IntegrationEvent;
