using System.Net;
using System.Net.Http.Json;
using AegisIdentity.DataAccess.Persistence;
using AegisIdentity.Domain.Security;
using AegisIdentity.Domain.Users;
using AegisIdentity.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AegisIdentity.IntegrationTests.Authorization;

[Collection(IntegrationCollection.Name)]
[Trait("Category", "Integration")]
public sealed class ChangePasswordEndpointTests
{
    private const string Endpoint = "/api/me/change-password";

    private readonly IntegrationFixture _fixture;

    public ChangePasswordEndpointTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    // ── Authentication enforcement ────────────────────────────────────────

    [Fact]
    public async Task Post_AnonymousRequest_Returns401()
    {
        var client = _fixture.CreateAnonymousClient();

        var response = await client.PostAsJsonAsync(Endpoint, new
        {
            currentPassword = "SomePass1!",
            newPassword = "AnotherPass1!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Current password incorrect ─────────────────────────────────────────

    [Fact]
    public async Task Post_WhenCurrentPasswordIsWrong_Returns400()
    {
        const string userId = "91000000-0000-0000-0000-000000000001";
        await SeedUserWithKnownPasswordAsync(userId);

        var client = _fixture.CreateAuthenticatedClient(userId);

        var response = await client.PostAsJsonAsync(Endpoint, new
        {
            currentPassword = "WrongPassword1!",
            newPassword = "NewStr0ng!Pass123"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── Happy path ────────────────────────────────────────────────────────

    [Fact]
    public async Task Post_WhenCurrentPasswordIsCorrect_Returns204()
    {
        const string userId = "91000000-0000-0000-0000-000000000002";
        var knownPassword = await SeedUserWithKnownPasswordAsync(userId);

        var client = _fixture.CreateAuthenticatedClient(userId);

        var response = await client.PostAsJsonAsync(Endpoint, new
        {
            currentPassword = knownPassword,
            newPassword = "NewStr0ng!Pass123"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Post_WhenCurrentPasswordIsCorrect_UpdatesPasswordHashInDatabase()
    {
        const string userId = "91000000-0000-0000-0000-000000000003";
        var knownPassword = await SeedUserWithKnownPasswordAsync(userId);

        await using var beforeScope = _fixture.Services.CreateAsyncScope();
        var beforeDb = beforeScope.ServiceProvider.GetRequiredService<AegisIdentityDbContext>();
        var originalHash = (await beforeDb.Users.IgnoreQueryFilters()
            .FirstAsync(u => u.Id == Guid.Parse(userId))).PasswordHash;

        var client = _fixture.CreateAuthenticatedClient(userId);
        await client.PostAsJsonAsync(Endpoint, new
        {
            currentPassword = knownPassword,
            newPassword = "NewStr0ng!Pass123"
        });

        await using var afterScope = _fixture.Services.CreateAsyncScope();
        var afterDb = afterScope.ServiceProvider.GetRequiredService<AegisIdentityDbContext>();
        var updatedUser = await afterDb.Users.IgnoreQueryFilters()
            .FirstAsync(u => u.Id == Guid.Parse(userId));

        updatedUser.PasswordHash.Should().NotBe(originalHash);
    }

    // ── Validation ────────────────────────────────────────────────────────

    [Fact]
    public async Task Post_WhenCurrentPasswordIsMissing_Returns400()
    {
        const string userId = "91000000-0000-0000-0000-000000000004";
        await SeedUserWithKnownPasswordAsync(userId);

        var client = _fixture.CreateAuthenticatedClient(userId);

        var response = await client.PostAsJsonAsync(Endpoint, new
        {
            currentPassword = "",
            newPassword = "NewStr0ng!Pass123"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_WhenNewPasswordIsMissing_Returns400()
    {
        const string userId = "91000000-0000-0000-0000-000000000005";
        var knownPassword = await SeedUserWithKnownPasswordAsync(userId);

        var client = _fixture.CreateAuthenticatedClient(userId);

        var response = await client.PostAsJsonAsync(Endpoint, new
        {
            currentPassword = knownPassword,
            newPassword = ""
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private async Task<string> SeedUserWithKnownPasswordAsync(string deterministicId)
    {
        const string knownPassword = "Kn0wn!P@ssword42";

        await using var scope = _fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AegisIdentityDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var userId = Guid.Parse(deterministicId);
        var existing = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId);

        if (existing is not null)
            return knownPassword;

        var hash = passwordHasher.Hash(knownPassword);
        var user = User.Create($"change-pw-{userId:N}@test.local", $"changepw-{userId:N}", hash);
        db.Users.Add(user);
        db.Entry(user).Property(u => u.Id).CurrentValue = userId;
        await db.SaveChangesAsync();

        return knownPassword;
    }
}
