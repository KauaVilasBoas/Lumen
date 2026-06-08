using MediatR;

namespace AegisIdentity.Domain.Audit;

public sealed record UserLoggedIn(Guid UserId, string Username) : INotification;
