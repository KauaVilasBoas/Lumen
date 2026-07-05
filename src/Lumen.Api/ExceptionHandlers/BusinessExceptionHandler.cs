using Lumen.Authorization.Exceptions;
using Lumen.SharedKernel.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Lumen.Api.ExceptionHandlers;

public sealed class BusinessExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken ct)
    {
        if (exception is BusinessException businessException)
        {
            httpContext.Response.StatusCode = businessException.StatusCode;
            httpContext.Response.ContentType = "application/problem+json";

            var problemDetails = BuildSharedKernelProblemDetails(httpContext, businessException);
            await httpContext.Response.WriteAsJsonAsync(problemDetails, ct);
            return true;
        }

        if (exception is AuthorizationException authorizationException)
        {
            httpContext.Response.StatusCode = authorizationException.StatusCode;
            httpContext.Response.ContentType = "application/problem+json";

            var problemDetails = new ProblemDetails
            {
                Status = authorizationException.StatusCode,
                Title = authorizationException.Message,
                Instance = httpContext.Request.Path,
            };

            await httpContext.Response.WriteAsJsonAsync(problemDetails, ct);
            return true;
        }

        return false;
    }

    private static ProblemDetails BuildSharedKernelProblemDetails(HttpContext httpContext, BusinessException exception)
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

        if (exception is AccountLockedException lockedEx)
        {
            var retryAfterSeconds = Math.Max(1, (int)(lockedEx.LockedUntil - DateTime.UtcNow).TotalSeconds);
            httpContext.Response.Headers.Append("Retry-After", retryAfterSeconds.ToString());
        }

        return new ProblemDetails
        {
            Status = exception.StatusCode,
            Title = exception.Message,
            Instance = httpContext.Request.Path,
        };
    }
}
