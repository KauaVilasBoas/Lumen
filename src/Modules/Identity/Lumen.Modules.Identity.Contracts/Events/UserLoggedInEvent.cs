using Lumen.Modularity;

namespace Lumen.Modules.Identity.Contracts.Events;

public sealed record UserLoggedInEvent(Guid UserId, string Username) : IntegrationEvent;
