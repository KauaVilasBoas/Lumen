using System.Net;
using FluentAssertions;

namespace AegisIdentity.IntegrationTests.Authorization;

[Trait("Category", "Integration")]
public sealed class FallbackAuthorizationPolicyTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public FallbackAuthorizationPolicyTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("POST", "/api/auth/register")]
    [InlineData("POST", "/api/auth/login")]
    [InlineData("POST", "/api/auth/refresh")]
    public async Task AnonymousEndpoints_WithNoToken_ReturnNonUnauthorized(string method, string path)
    {
        var client = _factory.CreateAnonymousClient();

        var response = await client.SendAsync(
            new HttpRequestMessage(new HttpMethod(method), path)
            {
                Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json"),
            });

        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized,
            $"{method} {path} is decorated with [AllowAnonymous] and must not require authentication");
    }

    [Fact]
    public async Task ProtectedEndpoint_WithNoToken_Returns401()
    {
        var client = _factory.CreateAnonymousClient();

        var response = await client.GetAsync("/api/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithValidToken_DoesNotReturn401()
    {
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/me");

        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized,
            "a valid bearer token must satisfy the fallback RequireAuthenticatedUser policy");
    }

    [Fact]
    public async Task HealthCheck_WithNoToken_Returns200()
    {
        var client = _factory.CreateAnonymousClient();

        var response = await client.GetAsync("/health/db");

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "/health/db is mapped with AllowAnonymous() and must not require authentication");
    }
}
