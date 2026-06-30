using System.Net.Http.Headers;
using FluentAssertions;
using Lumen.Modules.Identity.Infrastructure.Security;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lumen.IntegrationTests.Security;

// Hits the real HaveIBeenPwned API. Excluded from default runs via the ExternalApi trait so CI
// stays deterministic; run explicitly with:
//   dotnet test --filter "Category=ExternalApi"
[Trait("Category", "ExternalApi")]
public sealed class PwnedPasswordsClientIntegrationTests
{
    [Fact]
    public async Task IsPwnedAsync_ForCommonLeakedPassword_ReturnsTrue()
    {
        var client = CreateLiveClient();

        var result = await client.IsPwnedAsync("password");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsPwnedAsync_ForRandomStrongPassword_ReturnsFalse()
    {
        var client = CreateLiveClient();
        // Cryptographically unique value generated per test run — practically guaranteed to be absent from breaches.
        var password = "Lumen-Test-" + Guid.NewGuid().ToString("N");

        var result = await client.IsPwnedAsync(password);

        result.Should().BeFalse();
    }

    private static PwnedPasswordsClient CreateLiveClient()
    {
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://api.pwnedpasswords.com/"),
            Timeout = TimeSpan.FromSeconds(10),
        };

        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Lumen-IntegrationTests/1.0");
        httpClient.DefaultRequestHeaders.Add("Add-Padding", "true");
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));

        return new PwnedPasswordsClient(
            httpClient,
            new MemoryCache(new MemoryCacheOptions()),
            NullLogger<PwnedPasswordsClient>.Instance);
    }
}
