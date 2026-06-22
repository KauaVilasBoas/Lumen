using MediatR;

namespace Lumen.Domain.Authorization;

public sealed record UserPermissionsChanged(Guid UserId) : INotification;
