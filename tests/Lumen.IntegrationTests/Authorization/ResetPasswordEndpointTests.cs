using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using FluentAssertions;
using Lumen.IntegrationTests.Infrastructure;
using Lumen.Modules.Identity.Domain.Tokens;
using Lumen.SharedKernel.Util;
using Microsoft.EntityFrameworkCore;

namespace Lumen.IntegrationTests.Authorization;

[Collection(IntegrationCollection.Name)]
[Trait("Category", "Integration")]
public sealed class ResetPasswordEndpointTests
{
    private const string Endpoint = "/api/auth/reset-password";

    private readonly IntegrationFixture _fixture;

    public ResetPasswordEndpointTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Post_WhenTokenIsInvalid_Returns401()
    {
        var client = _fixture.CreateAnonymousClient();

        var response = await client.PostAsJsonAsync(Endpoint, new
        {
            token = "completely-invalid-token",
            newPassword = "NewStr0ng!Pass123"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Post_WhenTokenIsAlreadyUsed_Returns401()
    {
        var (_, rawToken) = await SeedUserWithResetTokenAsync("93000000-0000-0000-0000-000000000001");
        var client = _fixture.CreateAnonymousClient();

        await client.PostAsJsonAsync(Endpoint, new { token = rawToken, newPassword = "NewStr0ng!Pass123" });
        var secondResponse = await client.PostAsJsonAsync(Endpoint, new { token = rawToken, newPassword = "AnotherStr0ng!Pass123" });

        secondResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Post_WhenTokenIsValid_Returns204()
    {
        var (_, rawToken) = await SeedUserWithResetTokenAsync("93000000-0000-0000-0000-000000000002");
        var client = _fixture.CreateAnonymousClient();

        var response = await client.PostAsJsonAsync(Endpoint, new
        {
            token = rawToken,
            newPassword = "NewStr0ng!Pass123"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Post_WhenTokenIsValid_MarksTokenAsUsedInDatabase()
    {
        var (userId, rawToken) = await SeedUserWithResetTokenAsync("93000000-0000-0000-0000-000000000003");
        var client = _fixture.CreateAnonymousClient();

        await client.PostAsJsonAsync(Endpoint, new { token = rawToken, newPassword = "NewStr0ng!Pass123" });

        await using var db = _fixture.CreateIdentityDbContext();
        var tokenHash = Sha256Hasher.ComputeHex(rawToken);
        var token = await db.PasswordResetTokens
            .IgnoreQueryFilters()
            .FirstAsync(t => t.UserId == userId && t.TokenHash == tokenHash);

        token.IsUsed().Should().BeTrue();
    }

    [Fact]
    public async Task Post_WhenTokenIsValid_UpdatesPasswordHashInDatabase()
    {
        var (userId, rawToken) = await SeedUserWithResetTokenAsync("93000000-0000-0000-0000-000000000004");
        var client = _fixture.CreateAnonymousClient();

        await using var beforeDb = _fixture.CreateIdentityDbContext();
        var originalHash = (await beforeDb.Users.IgnoreQueryFilters().FirstAsync(u => u.Id == userId)).PasswordHash;

        await client.PostAsJsonAsync(Endpoint, new { token = rawToken, newPassword = "NewStr0ng!Pass123" });

        await using var afterDb = _fixture.CreateIdentityDbContext();
        var updatedUser = await afterDb.Users.IgnoreQueryFilters().FirstAsync(u => u.Id == userId);

        updatedUser.PasswordHash.Should().NotBe(originalHash);
    }

    [Fact]
    public async Task Post_WhenTokenIsMissing_Returns400()
    {
        var client = _fixture.CreateAnonymousClient();

        var response = await client.PostAsJsonAsync(Endpoint, new { token = "", newPassword = "NewStr0ng!Pass123" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_WhenNewPasswordIsMissing_Returns400()
    {
        var client = _fixture.CreateAnonymousClient();

        var response = await client.PostAsJsonAsync(Endpoint, new { token = "some-token", newPassword = "" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task<(Guid UserId, string RawToken)> SeedUserWithResetTokenAsync(string deterministicId)
    {
        await using var db = _fixture.CreateIdentityDbContext();

        var userId = Guid.Parse(deterministicId);
        var user = await AuthorizationSeeder.EnsureUserAsync(db, userId);

        var rawToken = GenerateRawToken();
        var tokenHash = Sha256Hasher.ComputeHex(rawToken);

        var resetToken = PasswordResetToken.Create(
            userId: user.Id,
            tokenHash: tokenHash,
            expiresAt: DateTime.UtcNow.AddMinutes(30));

        db.PasswordResetTokens.Add(resetToken);
        await db.SaveChangesAsync();

        return (userId, rawToken);
    }

    private static string GenerateRawToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Base64UrlEncoder.Encode(bytes);
    }
}
