using Lumen.Modularity;

namespace Lumen.Identity.Contracts.Events;

public sealed record UserLoggedInEvent(Guid UserId, string Username) : IntegrationEvent;
