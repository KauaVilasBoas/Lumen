using MediatR;

namespace Lumen.Domain.Audit;

public sealed record UserLoggedIn(Guid UserId, string Username) : INotification;
