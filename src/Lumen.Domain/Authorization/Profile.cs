using Lumen.Domain.Audit;
using Lumen.Domain.Common;
using Lumen.SharedKernel.Constants;
using Lumen.SharedKernel.Exceptions;
using Lumen.SharedKernel.Persistence;

namespace Lumen.Domain.Authorization;

public sealed class Profile : AggregateRoot, ISoftDeletable
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Name { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public bool IsSystem { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTime? DeletedAt { get; private set; }

    public static Profile Create(string name, string description, bool isSystem = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        return new Profile
        {
            Name = name,
            Description = description,
            IsSystem = isSystem,
        };
    }

    public void Update(string name, string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        Name = name;
        Description = description;
    }

    public void SoftDelete()
    {
        if (IsSystem)
            throw new ForbiddenException(BackofficeErrorMessages.SystemProfileCannotBeDeleted);

        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
    }

    public void Delete(IReadOnlyList<Guid> affectedUserIds)
    {
        SoftDelete();
        RaiseDomainEvent(new ProfileDeleted(Id, affectedUserIds));
    }

    public void RecordPermissionsSet(string actorUsername, IReadOnlyList<Guid> affectedUserIds)
    {
        RaiseDomainEvent(new ProfilePermissionsSet(Id, Name, actorUsername, affectedUserIds));
    }
}
