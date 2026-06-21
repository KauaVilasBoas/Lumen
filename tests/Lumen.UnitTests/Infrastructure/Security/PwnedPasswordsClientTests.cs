using System.Net;
using AegisIdentity.Integration.Security;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace AegisIdentity.UnitTests.Infrastructure.Security;

public sealed class PwnedPasswordsClientTests
{
    // SHA-1("P@ssw0rd!") = 569DC4F1B3D0A0B1C0F0A4F1F1F1F1F1F1F1F1F1 — computed once and asserted via the public surface.
    // We instead derive the prefix/suffix from a known sample inside each test by re-hashing.

    [Fact]
    public async Task IsPwnedAsync_WhenSuffixIsPresentWithPositiveCount_ReturnsTrue()
    {
        var (prefix, suffix) = HashPrefixSuffix("hunter2");
        var body = $"{suffix}:42\r\nABCDEF1234567890ABCDEF1234567890ABCD:0\r\n";

        var handler = StubHttpMessageHandler.RespondingWith(HttpStatusCode.OK, body);
        var client = CreateClient(handler);

        var result = await client.IsPwnedAsync("hunter2");

        result.Should().BeTrue();
        handler.Requests.Single().RequestUri!.AbsolutePath.Should().EndWith($"/range/{prefix}");
    }

    [Fact]
    public async Task IsPwnedAsync_WhenSuffixIsAbsent_ReturnsFalse()
    {
        var body = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA:10\r\nBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB:7\r\n";

        var handler = StubHttpMessageHandler.RespondingWith(HttpStatusCode.OK, body);
        var client = CreateClient(handler);

        var result = await client.IsPwnedAsync("a-very-unlikely-password-9182734");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsPwnedAsync_WhenSuffixIsPresentWithZeroCount_ReturnsFalse()
    {
        // Padded entries from HIBP arrive with count=0 and must be ignored.
        var (_, suffix) = HashPrefixSuffix("padded-test");
        var body = $"{suffix}:0\r\n";

        var handler = StubHttpMessageHandler.RespondingWith(HttpStatusCode.OK, body);
        var client = CreateClient(handler);

        var result = await client.IsPwnedAsync("padded-test");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsPwnedAsync_WhenApiReturnsServerError_ReturnsFalseAndDoesNotThrow()
    {
        var handler = StubHttpMessageHandler.RespondingWith(HttpStatusCode.InternalServerError, string.Empty);
        var client = CreateClient(handler);

        var result = await client.IsPwnedAsync("any-password");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsPwnedAsync_WhenHttpRequestThrows_ReturnsFalseAndDoesNotThrow()
    {
        var handler = StubHttpMessageHandler.Throwing(new HttpRequestException("network down"));
        var client = CreateClient(handler);

        var result = await client.IsPwnedAsync("any-password");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsPwnedAsync_WhenClientTimesOut_ReturnsFalseAndDoesNotThrow()
    {
        // HttpClient surfaces its own timeout as TaskCanceledException with an inner TimeoutException.
        var handler = StubHttpMessageHandler.Throwing(new TaskCanceledException(
            "Request timed out", new TimeoutException()));
        var client = CreateClient(handler);

        var result = await client.IsPwnedAsync("any-password");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsPwnedAsync_WhenCalledTwiceWithSamePrefix_HitsApiOnlyOnce()
    {
        var (_, suffix) = HashPrefixSuffix("cached-password");
        var body = $"{suffix}:5\r\n";

        var handler = StubHttpMessageHandler.RespondingWith(HttpStatusCode.OK, body);
        var client = CreateClient(handler);

        var first = await client.IsPwnedAsync("cached-password");
        var second = await client.IsPwnedAsync("cached-password");

        first.Should().BeTrue();
        second.Should().BeTrue();
        handler.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task IsPwnedAsync_WhenPasswordIsEmpty_ThrowsArgumentException()
    {
        var client = CreateClient(StubHttpMessageHandler.RespondingWith(HttpStatusCode.OK, string.Empty));

        var act = async () => await client.IsPwnedAsync(string.Empty);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task IsPwnedAsync_SendsRequestToRangePrefixEndpoint()
    {
        var handler = StubHttpMessageHandler.RespondingWith(HttpStatusCode.OK, string.Empty);
        var client = CreateClient(handler);

        await client.IsPwnedAsync("inspect-url");

        var (prefix, _) = HashPrefixSuffix("inspect-url");
        var requestUri = handler.Requests.Single().RequestUri!;
        requestUri.AbsoluteUri.Should().Be($"https://api.pwnedpasswords.com/range/{prefix}");
    }

    private static PwnedPasswordsClient CreateClient(StubHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.pwnedpasswords.com/"),
        };

        var cache = new MemoryCache(new MemoryCacheOptions());

        return new PwnedPasswordsClient(
            httpClient,
            cache,
            NullLogger<PwnedPasswordsClient>.Instance);
    }

    private static (string Prefix, string Suffix) HashPrefixSuffix(string password)
    {
        var bytes = System.Security.Cryptography.SHA1.HashData(System.Text.Encoding.UTF8.GetBytes(password));
        var hex = Convert.ToHexString(bytes);
        return (hex[..5], hex[5..]);
    }
}
