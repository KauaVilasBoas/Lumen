using MediatR;

namespace Lumen.Domain.Audit;

public sealed record CleanupJobExecuted(string JobName, long DeletedCount) : INotification;
