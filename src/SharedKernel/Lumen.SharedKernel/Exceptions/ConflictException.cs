namespace Lumen.SharedKernel.Exceptions;

public class ConflictException : BusinessException
{
    public ConflictException(string message) : base(message, 409) { }
}
