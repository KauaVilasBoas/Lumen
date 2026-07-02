using Lumen.Modularity;

namespace Lumen.Authorization.Contracts.Events;

public sealed record UserProfileRemovedEvent(
    Guid UserId,
    string Username,
    Guid ProfileId,
    string ProfileName) : IntegrationEvent;
