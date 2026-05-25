namespace AegisIdentity.SharedKernel.Exceptions;

public abstract class BusinessException : Exception
{
    public int StatusCode { get; }

    protected BusinessException(string message, int statusCode) : base(message)
    {
        StatusCode = statusCode;
    }
}
