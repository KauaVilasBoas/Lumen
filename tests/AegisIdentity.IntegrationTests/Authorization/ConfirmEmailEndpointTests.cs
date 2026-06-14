using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using AegisIdentity.DataAccess.Persistence;
using AegisIdentity.Domain.Tokens;
using AegisIdentity.Domain.Users;
using AegisIdentity.IntegrationTests.Infrastructure;
using AegisIdentity.SharedKernel.Util;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AegisIdentity.IntegrationTests.Authorization;

[Collection(IntegrationCollection.Name)]
[Trait("Category", "Integration")]
public sealed class ConfirmEmailEndpointTests
{
    private const string ConfirmEndpoint = "/api/auth/confirm-email";
    private const string ResendEndpoint = "/api/auth/resend-confirmation";

    private readonly IntegrationFixture _fixture;

    public ConfirmEmailEndpointTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    // ── GET /api/auth/confirm-email ───────────────────────────────────────

    [Fact]
    public async Task Get_WhenTokenIsValid_Returns200()
    {
        var (_, rawToken) = await SeedPendingUserWithTokenAsync("98000000-0000-0000-0000-000000000001");
        var client = _fixture.CreateAnonymousClient();

        var response = await client.GetAsync($"{ConfirmEndpoint}?token={Uri.EscapeDataString(rawToken)}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_WhenTokenIsValid_ActivatesUserInDatabase()
    {
        var (userId, rawToken) = await SeedPendingUserWithTokenAsync("98000000-0000-0000-0000-000000000002");
        var client = _fixture.CreateAnonymousClient();

        await client.GetAsync($"{ConfirmEndpoint}?token={Uri.EscapeDataString(rawToken)}");

        await using var scope = _fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AegisIdentityDbContext>();
        var user = await db.Users.IgnoreQueryFilters().FirstAsync(u => u.Id == userId);

        user.IsActive.Should().BeTrue();
        user.EmailConfirmedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Get_WhenTokenIsValid_MarksTokenAsUsedInDatabase()
    {
        var (userId, rawToken) = await SeedPendingUserWithTokenAsync("98000000-0000-0000-0000-000000000003");
        var client = _fixture.CreateAnonymousClient();

        await client.GetAsync($"{ConfirmEndpoint}?token={Uri.EscapeDataString(rawToken)}");

        await using var scope = _fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AegisIdentityDbContext>();
        var tokenHash = Sha256Hasher.ComputeHex(rawToken);
        var token = await db.EmailConfirmationTokens
            .IgnoreQueryFilters()
            .FirstAsync(t => t.UserId == userId && t.TokenHash == tokenHash);

        token.IsUsed().Should().BeTrue();
    }

    [Fact]
    public async Task Get_WhenTokenIsInvalid_Returns401()
    {
        var client = _fixture.CreateAnonymousClient();

        var response = await client.GetAsync($"{ConfirmEndpoint}?token=completely-invalid-token");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_WhenTokenIsAlreadyUsed_Returns401()
    {
        var (_, rawToken) = await SeedPendingUserWithTokenAsync("98000000-0000-0000-0000-000000000004");
        var client = _fixture.CreateAnonymousClient();

        await client.GetAsync($"{ConfirmEndpoint}?token={Uri.EscapeDataString(rawToken)}");
        var secondResponse = await client.GetAsync($"{ConfirmEndpoint}?token={Uri.EscapeDataString(rawToken)}");

        secondResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_WhenTokenIsMissing_Returns400()
    {
        var client = _fixture.CreateAnonymousClient();

        var response = await client.GetAsync($"{ConfirmEndpoint}?token=");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── POST /api/auth/resend-confirmation ────────────────────────────────

    [Fact]
    public async Task ResendPost_WhenEmailIsUnknown_Returns200()
    {
        var client = _fixture.CreateAnonymousClient();

        var response = await client.PostAsJsonAsync(ResendEndpoint, new { email = "ghost-resend@example.com" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ResendPost_WhenUserIsPending_Returns200()
    {
        var user = await SeedPendingUserOnlyAsync("98000000-0000-0000-0000-000000000005");
        var client = _fixture.CreateAnonymousClient();

        var response = await client.PostAsJsonAsync(ResendEndpoint, new { email = user.Email });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ResendPost_WhenUserIsPending_CreatesNewTokenInDatabase()
    {
        var user = await SeedPendingUserOnlyAsync("98000000-0000-0000-0000-000000000006");
        var client = _fixture.CreateAnonymousClient();

        await client.PostAsJsonAsync(ResendEndpoint, new { email = user.Email });

        await using var scope = _fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AegisIdentityDbContext>();
        var anyToken = await db.EmailConfirmationTokens.AnyAsync(t => t.UserId == user.Id);

        anyToken.Should().BeTrue();
    }

    [Fact]
    public async Task ResendPost_WhenEmailIsInvalid_Returns400()
    {
        var client = _fixture.CreateAnonymousClient();

        var response = await client.PostAsJsonAsync(ResendEndpoint, new { email = "notanemail" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private async Task<(Guid UserId, string RawToken)> SeedPendingUserWithTokenAsync(string deterministicId)
    {
        await using var scope = _fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AegisIdentityDbContext>();

        var userId = Guid.Parse(deterministicId);
        var user = await AuthorizationSeeder.EnsureUserAsync(db, userId);

        var rawToken = GenerateRawToken();
        var tokenHash = Sha256Hasher.ComputeHex(rawToken);

        var confirmationToken = EmailConfirmationToken.Create(
            userId: user.Id,
            tokenHash: tokenHash,
            expiresAt: DateTime.UtcNow.AddHours(24));

        db.EmailConfirmationTokens.Add(confirmationToken);
        await db.SaveChangesAsync();

        return (userId, rawToken);
    }

    private async Task<User> SeedPendingUserOnlyAsync(string deterministicId)
    {
        await using var scope = _fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AegisIdentityDbContext>();

        var userId = Guid.Parse(deterministicId);
        return await AuthorizationSeeder.EnsureUserAsync(db, userId);
    }

    private static string GenerateRawToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Base64UrlEncoder.Encode(bytes);
    }
}
