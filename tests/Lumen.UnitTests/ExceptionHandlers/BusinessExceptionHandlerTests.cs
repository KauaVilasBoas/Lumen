using Lumen.Api.ExceptionHandlers;
using Lumen.SharedKernel.Exceptions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;

namespace Lumen.UnitTests.ExceptionHandlers;

public sealed class BusinessExceptionHandlerTests
{
    // ── AccountLockedException — Retry-After header ───────────────────────

    [Fact]
    public async Task TryHandleAsync_WhenAccountLocked_SetsRetryAfterHeader()
    {
        var lockedUntil = DateTime.UtcNow.AddMinutes(10);
        var exception = new AccountLockedException(lockedUntil);
        var context = BuildHttpContext();
        var handler = new BusinessExceptionHandler();

        var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);

        handled.Should().BeTrue();
        context.Response.Headers.ContainsKey("Retry-After").Should().BeTrue();
    }

    [Fact]
    public async Task TryHandleAsync_WhenAccountLocked_RetryAfterValueIsAtLeastOne()
    {
        // Even if LockedUntil is already in the past, Retry-After must be >= 1.
        var lockedUntil = DateTime.UtcNow.AddSeconds(-5);
        var exception = new AccountLockedException(lockedUntil);
        var context = BuildHttpContext();
        var handler = new BusinessExceptionHandler();

        await handler.TryHandleAsync(context, exception, CancellationToken.None);

        var retryAfter = int.Parse(context.Response.Headers["Retry-After"].ToString());
        retryAfter.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task TryHandleAsync_WhenAccountLocked_RetryAfterApproximatesRemainingSeconds()
    {
        var lockedUntil = DateTime.UtcNow.AddMinutes(5);
        var exception = new AccountLockedException(lockedUntil);
        var context = BuildHttpContext();
        var handler = new BusinessExceptionHandler();

        await handler.TryHandleAsync(context, exception, CancellationToken.None);

        var retryAfter = int.Parse(context.Response.Headers["Retry-After"].ToString());
        // Allow ±2 s margin for test execution time
        retryAfter.Should().BeCloseTo(300, delta: 2);
    }

    [Fact]
    public async Task TryHandleAsync_WhenAccountLocked_Returns423StatusCode()
    {
        var exception = new AccountLockedException(DateTime.UtcNow.AddMinutes(1));
        var context = BuildHttpContext();
        var handler = new BusinessExceptionHandler();

        await handler.TryHandleAsync(context, exception, CancellationToken.None);

        context.Response.StatusCode.Should().Be(423);
    }

    // ── Non-locked business exceptions must NOT set Retry-After ──────────

    [Fact]
    public async Task TryHandleAsync_WhenUnauthorizedException_DoesNotSetRetryAfterHeader()
    {
        var exception = new UnauthorizedException("Invalid credentials.");
        var context = BuildHttpContext();
        var handler = new BusinessExceptionHandler();

        await handler.TryHandleAsync(context, exception, CancellationToken.None);

        context.Response.Headers.ContainsKey("Retry-After").Should().BeFalse();
    }

    // ── Non-business exceptions are not handled ───────────────────────────

    [Fact]
    public async Task TryHandleAsync_WhenNonBusinessException_ReturnsFalse()
    {
        var exception = new InvalidOperationException("unexpected");
        var context = BuildHttpContext();
        var handler = new BusinessExceptionHandler();

        var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);

        handled.Should().BeFalse();
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static DefaultHttpContext BuildHttpContext()
    {
        var context = new DefaultHttpContext();
        // Use a real response stream so WriteAsJsonAsync does not throw.
        context.Response.Body = new MemoryStream();
        return context;
    }
}
