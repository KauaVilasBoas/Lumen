namespace Lumen.Authorization.Exceptions;

public abstract class AuthorizationException : Exception
{
    public int StatusCode { get; }

    protected AuthorizationException(string message, int statusCode) : base(message)
    {
        StatusCode = statusCode;
    }
}
