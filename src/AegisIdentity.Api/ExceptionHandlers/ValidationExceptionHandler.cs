using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace AegisIdentity.Api.ExceptionHandlers;

/// <summary>
/// Converts a <see cref="FluentValidation.ValidationException"/> thrown by the
/// MediatR <c>ValidationBehavior</c> pipeline into a structured
/// <see cref="ValidationProblemDetails"/> (RFC 7807) response with HTTP 400.
/// </summary>
public sealed class ValidationExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken ct)
    {
        if (exception is not ValidationException validationException)
            return false;

        var errors = validationException.Errors
            .GroupBy(f => f.PropertyName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.Select(f => f.ErrorMessage).ToArray(),
                StringComparer.OrdinalIgnoreCase);

        var problemDetails = new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "One or more validation errors occurred.",
            Instance = httpContext.Request.Path,
        };

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(problemDetails, ct);

        return true;
    }
}
