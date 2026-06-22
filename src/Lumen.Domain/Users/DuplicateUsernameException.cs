namespace Lumen.Domain.Users;

public sealed class DuplicateUsernameException : Exception
{
    public DuplicateUsernameException(string username)
        : base($"A user with username '{username}' already exists.")
    {
    }
}
