using AegisIdentity.DataAccess.Persistence.Repositories;
using AegisIdentity.Domain.Tokens;
using AegisIdentity.Domain.Users;
using AegisIdentity.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace AegisIdentity.IntegrationTests.Persistence;

[Collection(IntegrationCollection.Name)]
[Trait("Category", "Integration")]
public sealed class RefreshTokenRepositoryIntegrationTests(IntegrationFixture fixture)
{
    [Fact]
    public async Task InsertAsync_ValidToken_PersistsToDatabase()
    {
        await using var dbContext = fixture.CreateDbContext();
        var userRepo = new UserRepository(dbContext);
        var tokenRepo = new RefreshTokenRepository(dbContext);

        var user = await CreateUserAsync(userRepo);
        var token = RefreshToken.Create(user.Id, $"hash-{Guid.NewGuid():N}", DateTime.UtcNow.AddHours(1), "127.0.0.1");
        await tokenRepo.InsertAsync(token);

        var found = await dbContext.RefreshTokens.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == token.Id);
        found.Should().NotBeNull();
    }

    [Fact]
    public async Task FindByTokenHashAsync_ExistingToken_ReturnsToken()
    {
        await using var dbContext = fixture.CreateDbContext();
        var userRepo = new UserRepository(dbContext);
        var tokenRepo = new RefreshTokenRepository(dbContext);

        var user = await CreateUserAsync(userRepo);
        var tokenHash = $"hash-{Guid.NewGuid():N}";
        var token = RefreshToken.Create(user.Id, tokenHash, DateTime.UtcNow.AddHours(1), "127.0.0.1");
        await tokenRepo.InsertAsync(token);

        var found = await tokenRepo.FindByTokenHashAsync(tokenHash);

        found.Should().NotBeNull();
        found!.TokenHash.Should().Be(tokenHash);
    }

    [Fact]
    public async Task FindByUserIdAsync_MultipleTokens_ReturnsAllForUser()
    {
        await using var dbContext = fixture.CreateDbContext();
        var userRepo = new UserRepository(dbContext);
        var tokenRepo = new RefreshTokenRepository(dbContext);

        var user = await CreateUserAsync(userRepo);
        await tokenRepo.InsertAsync(RefreshToken.Create(user.Id, $"h1-{Guid.NewGuid():N}", DateTime.UtcNow.AddHours(1), "127.0.0.1"));
        await tokenRepo.InsertAsync(RefreshToken.Create(user.Id, $"h2-{Guid.NewGuid():N}", DateTime.UtcNow.AddHours(1), "127.0.0.1"));

        var tokens = await tokenRepo.FindByUserIdAsync(user.Id);

        tokens.Should().HaveCountGreaterOrEqualTo(2);
    }

    [Fact]
    public async Task DeleteExpiredAsync_ExpiredTokens_SoftDeletesAndHidesFromQueries()
    {
        await using var dbContext = fixture.CreateDbContext();
        var userRepo = new UserRepository(dbContext);
        var tokenRepo = new RefreshTokenRepository(dbContext);

        var user = await CreateUserAsync(userRepo);
        var tokenHash = $"expired-{Guid.NewGuid():N}";
        var expiredToken = RefreshToken.Create(user.Id, tokenHash, DateTime.UtcNow.AddSeconds(1), "127.0.0.1");
        await tokenRepo.InsertAsync(expiredToken);

        var cutoff = DateTime.UtcNow.AddHours(1);
        var deleted = await tokenRepo.DeleteExpiredAsync(cutoff);

        deleted.Should().BeGreaterThanOrEqualTo(1);

        var notFound = await tokenRepo.FindByTokenHashAsync(tokenHash);
        notFound.Should().BeNull("global filter hides soft-deleted tokens");

        var stillInDb = await dbContext.RefreshTokens.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);
        stillInDb.Should().NotBeNull();
        stillInDb!.IsDeleted.Should().BeTrue();
    }

    private static async Task<User> CreateUserAsync(UserRepository userRepo)
    {
        var user = User.Create($"rt-{Guid.NewGuid():N}@test.com", $"rt-user-{Guid.NewGuid():N}", "hash");
        await userRepo.InsertAsync(user);
        return user;
    }
}
