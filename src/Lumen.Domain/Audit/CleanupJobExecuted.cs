using MediatR;

namespace AegisIdentity.Domain.Audit;

public sealed record CleanupJobExecuted(string JobName, long DeletedCount) : INotification;
