using System.Net;

using AegisIdentity.Backoffice.Models;

namespace AegisIdentity.Backoffice.Services;

public sealed class AuthApiClient
{
    private readonly HttpClient _http;

    public AuthApiClient(HttpClient http) => _http = http;

    public abstract class LoginResult
    {
        private LoginResult() { }

        public sealed class Success : LoginResult
        {
            public LoginResponse Response { get; }
            public Success(LoginResponse response) => Response = response;
        }

        public sealed class InvalidCredentials : LoginResult
        {
        }

        public sealed class EmailNotConfirmed : LoginResult
        {
        }

        public sealed class AccountLocked : LoginResult
        {
        }

        public sealed class ApiError : LoginResult
        {
            public HttpStatusCode StatusCode { get; }
            public ApiError(HttpStatusCode statusCode) => StatusCode = statusCode;
        }
    }

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

            // 423 Locked is not a standard HttpStatusCode enum member in all runtimes.
            (HttpStatusCode)423 => new LoginResult.AccountLocked(),

            _ => new LoginResult.ApiError(response.StatusCode),
        };
    }
}