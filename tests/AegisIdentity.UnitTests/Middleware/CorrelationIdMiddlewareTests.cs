using AegisIdentity.Api.Middleware;
using FluentAssertions;
using Microsoft.AspNetCore.Http;

namespace AegisIdentity.UnitTests.Middleware;

public sealed class CorrelationIdMiddlewareTests
{
    private const string CorrelationIdHeader = "X-Correlation-Id";

    [Fact]
    public async Task InvokeAsync_WhenNoHeaderPresent_GeneratesNewCorrelationId()
    {
        var context = new DefaultHttpContext();
        var middleware = new CorrelationIdMiddleware(BuildNextDelegate());

        await middleware.InvokeAsync(context);

        context.Response.Headers[CorrelationIdHeader].ToString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task InvokeAsync_WhenHeaderPresent_PreservesCorrelationId()
    {
        const string existingId = "abc-123-preserved";
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdHeader] = existingId;
        var middleware = new CorrelationIdMiddleware(BuildNextDelegate());

        await middleware.InvokeAsync(context);

        context.Response.Headers[CorrelationIdHeader].ToString().Should().Be(existingId);
    }

    [Fact]
    public async Task InvokeAsync_Always_SetsResponseHeader()
    {
        var context = new DefaultHttpContext();
        var middleware = new CorrelationIdMiddleware(BuildNextDelegate());

        await middleware.InvokeAsync(context);

        context.Response.Headers.ContainsKey(CorrelationIdHeader).Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_WhenNoHeaderPresent_GeneratedIdHas32HexChars()
    {
        var context = new DefaultHttpContext();
        var middleware = new CorrelationIdMiddleware(BuildNextDelegate());

        await middleware.InvokeAsync(context);

        var id = context.Response.Headers[CorrelationIdHeader].ToString();
        id.Should().HaveLength(32).And.MatchRegex("^[0-9a-f]{32}$");
    }

    private static RequestDelegate BuildNextDelegate()
        => _ => Task.CompletedTask;
}
