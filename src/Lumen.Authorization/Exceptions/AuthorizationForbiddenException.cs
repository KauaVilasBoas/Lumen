namespace Lumen.Authorization.Exceptions;

public sealed class AuthorizationForbiddenException : AuthorizationException
{
    public AuthorizationForbiddenException(string message) : base(message, 403) { }
}
