using MediatR;

namespace AegisIdentity.Domain.Audit;

public sealed record ProfilePermissionsSet(Guid ProfileId, string ProfileName, string ActorUsername) : INotification;
