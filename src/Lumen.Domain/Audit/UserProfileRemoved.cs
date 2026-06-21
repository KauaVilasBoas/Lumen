using MediatR;

namespace AegisIdentity.Domain.Audit;

public sealed record UserProfileRemoved(Guid UserId, string Username, Guid ProfileId, string ProfileName) : INotification;
