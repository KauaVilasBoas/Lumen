namespace Lumen.SharedKernel.Exceptions;

public sealed class UnauthorizedException : BusinessException
{
    public UnauthorizedException(string message) : base(message, 401) { }
}
