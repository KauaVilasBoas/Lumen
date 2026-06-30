using Lumen.SharedKernel.Exceptions;

namespace Lumen.Modules.Identity.Domain.Users;

internal sealed class DuplicateUsernameException : ConflictException
{
    public DuplicateUsernameException(string username)
        : base($"A user with username '{username}' already exists.")
    {
    }
}
