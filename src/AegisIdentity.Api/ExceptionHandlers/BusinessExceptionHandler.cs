using AegisIdentity.SharedKernel.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace AegisIdentity.Api.ExceptionHandlers;

public sealed class BusinessExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken ct)
    {
        if (exception is not BusinessException businessException)
            return false;

        httpContext.Response.StatusCode = businessException.StatusCode;
        httpContext.Response.ContentType = "application/problem+json";

        var problemDetails = BuildProblemDetails(httpContext, businessException);

        await httpContext.Response.WriteAsJsonAsync(problemDetails, ct);

        return true;
    }

    private static ProblemDetails BuildProblemDetails(HttpContext httpContext, BusinessException exception)
    {
        if (exception is SharedKernel.Exceptions.ValidationException validationException)
        {
            return new ValidationProblemDetails(
                validationException.Errors.ToDictionary(kv => kv.Key, kv => kv.Value))
            {
                Status = exception.StatusCode,
                Title = exception.Message,
                Instance = httpContext.Request.Path,
            };
        }

        return new ProblemDetails
        {
            Status = exception.StatusCode,
            Title = exception.Message,
            Instance = httpContext.Request.Path,
        };
    }
}
