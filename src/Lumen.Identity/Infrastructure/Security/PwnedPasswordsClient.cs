using System.Security.Cryptography;
using System.Text;
using Lumen.Identity.Domain.Security;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Lumen.Identity.Infrastructure.Security;

internal sealed class PwnedPasswordsClient : IPwnedPasswordsClient
{
    internal const string CacheKeyPrefix = "hibp:range:";
    internal static readonly TimeSpan CacheTtl = TimeSpan.FromHours(1);

    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<PwnedPasswordsClient> _logger;

    public PwnedPasswordsClient(
        HttpClient httpClient,
        IMemoryCache cache,
        ILogger<PwnedPasswordsClient> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
    }

    public async Task<bool> IsPwnedAsync(string password, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);

        var (prefix, suffix) = HashAndSplit(password);

        try
        {
            var rangeResponse = await GetRangeAsync(prefix, ct);
            return ContainsSuffix(rangeResponse, suffix);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning("HIBP range lookup timed out; failing open and accepting the password.");
            return false;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "HIBP range lookup failed ({StatusCode}); failing open.", ex.StatusCode);
            return false;
        }
    }

    private async Task<string> GetRangeAsync(string prefix, CancellationToken ct)
    {
        var cacheKey = CacheKeyPrefix + prefix;
        if (_cache.TryGetValue(cacheKey, out string? cached) && cached is not null)
            return cached;

        using var response = await _httpClient.GetAsync($"range/{prefix}", ct);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync(ct);

        _cache.Set(cacheKey, body, CacheTtl);
        return body;
    }

    private static (string Prefix, string Suffix) HashAndSplit(string password)
    {
        var bytes = SHA1.HashData(Encoding.UTF8.GetBytes(password));
        var hex = Convert.ToHexString(bytes);
        return (hex[..5], hex[5..]);
    }

    private static bool ContainsSuffix(string rangeResponse, string suffix)
    {
        foreach (var line in rangeResponse.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = line.Trim();
            var separator = trimmed.IndexOf(':');
            if (separator <= 0)
                continue;

            var candidateSuffix = trimmed[..separator];
            if (!candidateSuffix.Equals(suffix, StringComparison.OrdinalIgnoreCase))
                continue;

            var countSpan = trimmed.AsSpan(separator + 1);
            return int.TryParse(countSpan, out var count) && count > 0;
        }

        return false;
    }
}
