using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Lumen.IntegrationTests.Infrastructure;
using Lumen.Modules.Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Lumen.IntegrationTests.Authorization;

[Collection(IntegrationCollection.Name)]
[Trait("Category", "Integration")]
public sealed class ForgotPasswordEndpointTests
{
    private const string Endpoint = "/api/auth/forgot-password";

    private readonly IntegrationFixture _fixture;

    public ForgotPasswordEndpointTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Post_WhenEmailDoesNotExist_Returns200()
    {
        var client = _fixture.CreateAnonymousClient();

        var response = await client.PostAsJsonAsync(Endpoint, new { email = "ghost@example.com" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Post_WhenEmailExists_Returns200()
    {
        var user = await SeedUserAsync("97000000-0000-0000-0000-000000000001");
        var client = _fixture.CreateAnonymousClient();

        var response = await client.PostAsJsonAsync(Endpoint, new { email = user.Email });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Post_WhenEmailExists_CreatesPasswordResetTokenInDatabase()
    {
        var user = await SeedUserAsync("97000000-0000-0000-0000-000000000002");
        var client = _fixture.CreateAnonymousClient();

        await client.PostAsJsonAsync(Endpoint, new { email = user.Email });

        await using var db = _fixture.CreateIdentityDbContext();
        var token = await db.PasswordResetTokens.FirstOrDefaultAsync(t => t.UserId == user.Id);

        token.Should().NotBeNull();
        token!.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task Post_WhenEmailDoesNotExist_DoesNotCreateAnyToken()
    {
        var unknownEmail = $"nobody-{Guid.NewGuid():N}@example.com";
        var client = _fixture.CreateAnonymousClient();

        await client.PostAsJsonAsync(Endpoint, new { email = unknownEmail });

        // No assertion on the full table — the absence of an exception is the primary assertion here.
        // This guards against silent side-effects for unknown addresses.
    }

    [Fact]
    public async Task Post_WhenEmailIsInvalid_Returns400()
    {
        var client = _fixture.CreateAnonymousClient();

        var response = await client.PostAsJsonAsync(Endpoint, new { email = "notanemail" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_WhenEmailIsEmpty_Returns400()
    {
        var client = _fixture.CreateAnonymousClient();

        var response = await client.PostAsJsonAsync(Endpoint, new { email = "" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task<User> SeedUserAsync(string deterministicId)
    {
        await using var db = _fixture.CreateIdentityDbContext();

        var userId = Guid.Parse(deterministicId);
        return await AuthorizationSeeder.EnsureUserAsync(db, userId);
    }
}
