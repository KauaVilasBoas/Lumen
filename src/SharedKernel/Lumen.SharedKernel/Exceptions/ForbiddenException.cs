namespace Lumen.SharedKernel.Exceptions;

public sealed class ForbiddenException : BusinessException
{
    public ForbiddenException(string message) : base(message, 403) { }
}
