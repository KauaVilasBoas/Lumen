using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Lumen.IntegrationTests.Infrastructure;

namespace Lumen.IntegrationTests.Auth;

[Collection(IntegrationCollection.Name)]
[Trait("Category", "Integration")]
public sealed class AuthEndpointTests
{
    private readonly IntegrationFixture _fixture;

    public AuthEndpointTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Login_MissingCredentials_Returns400WithValidationProblem()
    {
        var client = _fixture.CreateAnonymousClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            Identifier = string.Empty,
            Password = string.Empty,
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_NonExistentUser_Returns401()
    {
        var client = _fixture.CreateAnonymousClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            Identifier = $"ghost-{Guid.NewGuid():N}@nowhere.test",
            Password = "SomePassword123!",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Register_InvalidEmail_Returns400()
    {
        var client = _fixture.CreateAnonymousClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            Email = "not-an-email",
            Username = $"validuser{Guid.NewGuid():N}"[..20],
            Password = "Password123!",
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ForgotPassword_AnyEmail_Returns200ToPreventEnumeration()
    {
        var client = _fixture.CreateAnonymousClient();

        var response = await client.PostAsJsonAsync("/api/auth/forgot-password", new
        {
            Email = $"ghost-{Guid.NewGuid():N}@nowhere.test",
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "forgot-password must return 200 for any address to prevent account enumeration");
    }

    [Fact]
    public async Task Logout_AnonymousRequest_Returns401()
    {
        var client = _fixture.CreateAnonymousClient();

        var response = await client.PostAsJsonAsync("/api/auth/logout", new
        {
            RefreshToken = (string?)null,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
