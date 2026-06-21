using MediatR;

namespace AegisIdentity.Domain.Authorization;

public sealed record UserPermissionsChanged(Guid UserId) : INotification;
