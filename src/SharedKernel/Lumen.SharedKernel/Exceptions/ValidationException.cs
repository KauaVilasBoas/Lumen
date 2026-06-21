namespace Lumen.SharedKernel.Exceptions;

public sealed class ValidationException : BusinessException
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ValidationException(string field, IEnumerable<string> errors)
        : base("One or more validation errors occurred.", 400)
    {
        Errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            [field] = errors.ToArray()
        };
    }

    public ValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.", 400)
    {
        Errors = errors;
    }
}
