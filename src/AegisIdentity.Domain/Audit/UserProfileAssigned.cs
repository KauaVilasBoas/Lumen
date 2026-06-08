using MediatR;

namespace AegisIdentity.Domain.Audit;

public sealed record UserProfileAssigned(Guid UserId, string Username, Guid ProfileId, string ProfileName) : INotification;
