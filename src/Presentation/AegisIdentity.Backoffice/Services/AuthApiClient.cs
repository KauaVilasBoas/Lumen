using System.Net;
using System.Net.Http.Json;
using AegisIdentity.Backoffice.Models;

namespace AegisIdentity.Backoffice.Services;

/// <summary>
/// Typed HttpClient responsible for authentication-related calls to the AegisIdentity Api.
/// Registered via <c>AddHttpClient&lt;AuthApiClient&gt;</c> in Program.cs so the
/// <see cref="HttpClient"/> instance is managed by <see cref="IHttpClientFactory"/>.
/// </summary>
public sealed class AuthApiClient
{
    private readonly HttpClient _http;

    public AuthApiClient(HttpClient http) => _http = http;

    /// <summary>
    /// Represents the outcome of an Api login attempt.
    /// </summary>
    public abstract class LoginResult
    {
        private LoginResult() { }

        /// <summary>Api returned 200 with token payload.</summary>
        public sealed class Success : LoginResult
        {
            public LoginResponse Response { get; }
            public Success(LoginResponse response) => Response = response;
        }

        /// <summary>Api returned 401 — wrong credentials.</summary>
        public sealed class InvalidCredentials : LoginResult { }

        /// <summary>Api returned 403 — email not confirmed.</summary>
        public sealed class EmailNotConfirmed : LoginResult { }

        /// <summary>Api returned 423 — account locked.</summary>
        public sealed class AccountLocked : LoginResult { }

        /// <summary>Unexpected status code from the Api.</summary>
        public sealed class ApiError : LoginResult
        {
            public HttpStatusCode StatusCode { get; }
            public ApiError(HttpStatusCode statusCode) => StatusCode = statusCode;
        }
    }

    /// <summary>
    /// Posts credentials to <c>POST /api/auth/login</c> and returns a typed result.
    /// HttpClient is configured with <c>BaseAddress</c> from <c>Api:BaseUrl</c>.
    /// </summary>
    public async Task<LoginResult> LoginAsync(
        string identifier,
        string password,
        CancellationToken ct = default)
    {
        var request = new LoginRequest(identifier, password);

        using var response = await _http.PostAsJsonAsync("api/auth/login", request, ct);

        return response.StatusCode switch
        {
            HttpStatusCode.OK =>
                new LoginResult.Success(
                    await response.Content.ReadFromJsonAsync<LoginResponse>(ct)
                    ?? throw new InvalidOperationException("Api login returned null body on 200.")),

            HttpStatusCode.Unauthorized => new LoginResult.InvalidCredentials(),

            HttpStatusCode.Forbidden => new LoginResult.EmailNotConfirmed(),

            // 423 Locked is not a standard HttpStatusCode enum member in all runtimes —
            // use the numeric value to stay portable.
            (HttpStatusCode)423 => new LoginResult.AccountLocked(),

            _ => new LoginResult.ApiError(response.StatusCode),
        };
    }
}
