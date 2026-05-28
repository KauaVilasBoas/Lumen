namespace AegisIdentity.SharedKernel.Exceptions;

public sealed class ConflictException : BusinessException
{
    public ConflictException(string message) : base(message, 409) { }
}
