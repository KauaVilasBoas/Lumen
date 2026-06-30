namespace Lumen.Modules.Audit.Domain;

internal sealed class AuditEntry
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Kind { get; init; } = string.Empty;

    public string? Actor { get; init; }

    public string? Target { get; init; }

    public string Message { get; init; } = string.Empty;

    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;

    internal static AuditEntry Create(string kind, string? actor, string? target, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(kind);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        return new AuditEntry
        {
            Kind       = kind,
            Actor      = actor,
            Target     = target,
            Message    = message,
            OccurredAt = DateTime.UtcNow,
        };
    }
}
