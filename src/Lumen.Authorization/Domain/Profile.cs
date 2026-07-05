using Lumen.Authorization.Exceptions;
using Lumen.Authorization.Internal;
using Lumen.Authorization.Persistence;

namespace Lumen.Authorization.Domain;

public sealed class Profile : ISoftDeletable
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
            throw new AuthorizationForbiddenException(AuthorizationErrorMessages.SystemProfileCannotBeDeleted);

        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
    }
}
