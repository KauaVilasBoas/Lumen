using MediatR;

namespace Lumen.Domain.Audit;

public sealed record ProfilePermissionsSet(Guid ProfileId, string ProfileName, string ActorUsername) : INotification;
