namespace AegisIdentity.SharedKernel.Util;

/// <summary>
/// URL-safe Base64 encoding without padding characters.
/// RFC 4648 §5 — replaces <c>+</c> with <c>-</c>, <c>/</c> with <c>_</c>,
/// and strips trailing <c>=</c> padding so the output is safe for use in
/// URLs and HTTP headers without percent-encoding.
/// </summary>
/// <remarks>
/// Intentionally implemented without a dependency on
/// <c>Microsoft.AspNetCore.WebUtilities</c> so this helper can be used in
/// non-web projects (Application, Jobs) without pulling in ASP.NET Core.
/// </remarks>
public static class Base64UrlEncoder
{
    /// <summary>
    /// Encodes <paramref name="bytes"/> as a URL-safe, unpadded Base64 string.
    /// </summary>
    /// <param name="bytes">The raw bytes to encode. Must not be <see langword="null"/>.</param>
    /// <returns>A URL-safe Base64 string with no padding characters.</returns>
    public static string Encode(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);

        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
