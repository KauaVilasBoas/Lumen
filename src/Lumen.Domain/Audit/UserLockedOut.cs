using MediatR;

namespace Lumen.Domain.Audit;

public sealed record UserLockedOut(Guid UserId, string Username) : INotification;
