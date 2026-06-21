using Serilog.Context;

namespace AegisIdentity.Api.Middleware;

/// <summary>
/// Reads or generates a correlation id per request and propagates it via the
/// <c>X-Correlation-Id</c> response header and the Serilog <see cref="LogContext"/>.
/// Must be registered before <c>UseSerilogRequestLogging</c>.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    private const string CorrelationIdHeader = "X-Correlation-Id";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault()
                            ?? Guid.NewGuid().ToString("N");

        context.Response.Headers[CorrelationIdHeader] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}
