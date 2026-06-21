namespace AegisIdentity.SharedKernel.Exceptions;

public sealed class NotFoundException : BusinessException
{
    public NotFoundException(string message) : base(message, 404) { }
}
