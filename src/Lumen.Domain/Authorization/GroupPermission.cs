using AegisIdentity.SharedKernel.Persistence;

namespace AegisIdentity.Domain.Authorization;

public sealed class GroupPermission : ISoftDeletable
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Name { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public bool IsDeleted { get; private set; }

    public DateTime? DeletedAt { get; private set; }

    public static GroupPermission Create(string name, string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        return new GroupPermission
        {
            Name = name,
            Description = description,
        };
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
    }
}
