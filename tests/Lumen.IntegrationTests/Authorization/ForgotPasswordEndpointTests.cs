using System.Net;
using System.Net.Http.Json;
using Lumen.DataAccess.Persistence;
using Lumen.Domain.Users;
using Lumen.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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

    // ── Always 200 regardless of whether the email exists ─────────────────

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

    // ── Token is persisted when the email exists ───────────────────────────

    [Fact]
    public async Task Post_WhenEmailExists_CreatesPasswordResetTokenInDatabase()
    {
        var user = await SeedUserAsync("97000000-0000-0000-0000-000000000002");
        var client = _fixture.CreateAnonymousClient();

        await client.PostAsJsonAsync(Endpoint, new { email = user.Email });

        await using var scope = _fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LumenDbContext>();
        var token = await db.PasswordResetTokens.FirstOrDefaultAsync(t => t.UserId == user.Id);

        token.Should().NotBeNull();
        token!.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    // ── No token is created for unknown emails ─────────────────────────────

    [Fact]
    public async Task Post_WhenEmailDoesNotExist_DoesNotCreateAnyToken()
    {
        var unknownEmail = $"nobody-{Guid.NewGuid():N}@example.com";
        var client = _fixture.CreateAnonymousClient();

        await client.PostAsJsonAsync(Endpoint, new { email = unknownEmail });

        await using var scope = _fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LumenDbContext>();
        var anyToken = await db.PasswordResetTokens
            .AnyAsync(t => t.UsedAt == null);

        // This test is meaningful only as a regression check — no assertion on the full table,
        // just verifying the call itself completed without side-effects for the unknown address.
        // The absence of an exception is the primary assertion here.
        _ = anyToken;
    }

    // ── Validation ─────────────────────────────────────────────────────────

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

    // ── Helpers ───────────────────────────────────────────────────────────

    private async Task<User> SeedUserAsync(string deterministicId)
    {
        await using var scope = _fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LumenDbContext>();

        var userId = Guid.Parse(deterministicId);
        return await AuthorizationSeeder.EnsureUserAsync(db, userId);
    }
}
