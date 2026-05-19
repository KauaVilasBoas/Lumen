using Serilog.Context;

namespace AegisIdentity.Api.Middleware;

/// <summary>
/// Reads or generates a correlation ID for each request and propagates it through:
/// <list type="bullet">
///   <item>The <c>X-Correlation-Id</c> response header.</item>
///   <item>The Serilog <see cref="LogContext"/> so every log entry in the request scope
///         includes a <c>CorrelationId</c> property.</item>
/// </list>
/// This middleware must be registered before <c>UseSerilogRequestLogging</c> so that
/// the request-completion log entry also carries the correlation ID.
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
