using Lumen.Modularity;

namespace Lumen.Modules.Audit.Contracts.Events;

public sealed record CleanupJobExecutedEvent(string JobName, long DeletedCount) : IntegrationEvent;
