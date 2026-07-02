namespace Lumen.Authorization.Exceptions;

public sealed class AuthorizationNotFoundException : AuthorizationException
{
    public AuthorizationNotFoundException(string message) : base(message, 404) { }
}
