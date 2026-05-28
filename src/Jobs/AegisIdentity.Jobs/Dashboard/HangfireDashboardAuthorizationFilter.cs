using System.Security.Cryptography;
using System.Text;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace AegisIdentity.Jobs.Dashboard;

/// <summary>
/// Hangfire dashboard authorization filter that enforces HTTP Basic
/// Authentication using credentials stored in
/// <see cref="HangfireDashboardOptions"/>.
///
/// Security properties:
/// <list type="bullet">
///   <item>Credentials are compared with
///         <see cref="CryptographicOperations.FixedTimeEquals"/> (constant-time
///         byte comparison) to prevent timing-based enumeration attacks.</item>
///   <item>Fail-closed: if either Username or Password is empty (placeholder
///         not replaced), the filter denies access and returns 401.</item>
///   <item>Unauthenticated requests receive a 401 response with a
///         <c>WWW-Authenticate: Basic realm="Hangfire"</c> header to trigger
///         the browser's built-in credentials dialog.</item>
/// </list>
///
/// Configuration (appsettings / user-secrets / env vars):
/// <code>
///   "Hangfire": {
///     "Dashboard": {
///       "Username": "&lt;set via user-secrets or env&gt;",
///       "Password": "&lt;set via user-secrets or env&gt;",
///       "Path":     "/internal/jobs-admin"
///     }
///   }
/// </code>
/// </summary>
public sealed class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    private readonly HangfireDashboardOptions _options;

    public HangfireDashboardAuthorizationFilter(IOptions<HangfireDashboardOptions> options)
    {
        _options = options.Value;
    }

    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        // Fail-closed: placeholder credentials must be replaced before access is granted.
        if (string.IsNullOrEmpty(_options.Username) || string.IsNullOrEmpty(_options.Password))
        {
            Challenge(httpContext);
            return false;
        }

        var authHeader = httpContext.Request.Headers.Authorization.ToString();

        if (!authHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            Challenge(httpContext);
            return false;
        }

        string decoded;
        try
        {
            var encoded = authHeader["Basic ".Length..].Trim();
            decoded = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
        }
        catch (FormatException)
        {
            Challenge(httpContext);
            return false;
        }

        var separatorIndex = decoded.IndexOf(':');
        if (separatorIndex < 0)
        {
            Challenge(httpContext);
            return false;
        }

        var providedUsername = decoded[..separatorIndex];
        var providedPassword = decoded[(separatorIndex + 1)..];

        var usernameMatch = FixedTimeEquals(_options.Username, providedUsername);
        var passwordMatch = FixedTimeEquals(_options.Password, providedPassword);

        // Evaluate both comparisons unconditionally before short-circuiting
        // to avoid leaking which field was wrong via timing differences.
        if (!usernameMatch || !passwordMatch)
        {
            Challenge(httpContext);
            return false;
        }

        return true;
    }

    private static void Challenge(HttpContext httpContext)
    {
        httpContext.Response.Headers.WWWAuthenticate = "Basic realm=\"Hangfire\"";
        httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
    }

    /// <summary>
    /// Constant-time string comparison that encodes both strings to UTF-8
    /// before delegating to <see cref="CryptographicOperations.FixedTimeEquals"/>.
    ///
    /// Strings of different lengths cannot be equal, but we still compare the
    /// first string against itself to preserve constant time regardless of length.
    /// </summary>
    private static bool FixedTimeEquals(string expected, string provided)
    {
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var providedBytes = Encoding.UTF8.GetBytes(provided);

        if (expectedBytes.Length != providedBytes.Length)
        {
            // Perform a dummy comparison to avoid early-exit timing leakage.
            CryptographicOperations.FixedTimeEquals(expectedBytes, expectedBytes);
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
    }
}
