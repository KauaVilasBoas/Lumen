using Lumen.SharedKernel.Exceptions;

namespace Lumen.Domain.Users;

public sealed class DuplicateUsernameException : ConflictException
{
    public DuplicateUsernameException(string username)
        : base($"A user with username '{username}' already exists.")
    {
    }
}
