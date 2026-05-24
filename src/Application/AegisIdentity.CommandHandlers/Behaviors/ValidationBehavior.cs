using FluentValidation;
using MediatR;

namespace AegisIdentity.CommandHandlers.Behaviors;

/// <summary>
/// MediatR pipeline behavior that runs all registered <see cref="IValidator{T}"/> instances
/// for a request before the handler is invoked (fail-fast input validation).
/// <para>
/// Validators resolved here are responsible only for structural, non-I/O rules.
/// Business-rule validations that require I/O (repository lookups, external calls)
/// remain inside the handler's <c>Handle</c> method.
/// </para>
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        => _validators = validators;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);

        var failures = (await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, ct))))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count != 0)
            throw new ValidationException(failures);

        return await next();
    }
}
