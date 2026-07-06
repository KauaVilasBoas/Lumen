using Lumen.SharedKernel.Exceptions;

namespace Lumen.Identity.Domain.Users;

public sealed class DuplicateEmailException : ConflictException
{
    public DuplicateEmailException(string email)
        : base($"A user with email '{email}' already exists.")
    {
    }
}
