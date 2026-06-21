using MediatR;

namespace AegisIdentity.Domain.Audit;

public sealed record UserLockedOut(Guid UserId, string Username) : INotification;
