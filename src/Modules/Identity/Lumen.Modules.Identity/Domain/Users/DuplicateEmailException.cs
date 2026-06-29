using Lumen.SharedKernel.Exceptions;

namespace Lumen.Modules.Identity.Domain.Users;

internal sealed class DuplicateEmailException : ConflictException
{
    public DuplicateEmailException(string email)
        : base($"A user with email '{email}' already exists.")
    {
    }
}
