namespace Lumen.Authorization.Exceptions;

public sealed class AuthorizationConflictException : AuthorizationException
{
    public AuthorizationConflictException(string message) : base(message, 409) { }
}
