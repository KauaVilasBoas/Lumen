using MediatR;

namespace Lumen.Domain.Authorization;

public sealed record ProfileDeleted(Guid ProfileId, IReadOnlyList<Guid> AffectedUserIds) : INotification;
