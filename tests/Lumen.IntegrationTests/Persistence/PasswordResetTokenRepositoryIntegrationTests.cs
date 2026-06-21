using Lumen.DataAccess.Persistence.Repositories;
using Lumen.Domain.Tokens;
using Lumen.Domain.Users;
using Lumen.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Lumen.IntegrationTests.Persistence;

[Collection(IntegrationCollection.Name)]
[Trait("Category", "Integration")]
public sealed class PasswordResetTokenRepositoryIntegrationTests(IntegrationFixture fixture)
{
    [Fact]
    public async Task InsertAsync_ValidToken_PersistsToDatabase()
    {
        await using var dbContext = fixture.CreateDbContext();
        var userRepo = new UserRepository(dbContext);
        var tokenRepo = new PasswordResetTokenRepository(dbContext);

        var user = await CreateUserAsync(userRepo);
        var token = PasswordResetToken.Create(user.Id, $"hash-{Guid.NewGuid():N}", DateTime.UtcNow.AddHours(1));
        await tokenRepo.InsertAsync(token);

        var found = await dbContext.PasswordResetTokens.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == token.Id);
        found.Should().NotBeNull();
    }

    [Fact]
    public async Task FindByTokenHashAsync_ExistingToken_ReturnsToken()
    {
        await using var dbContext = fixture.CreateDbContext();
        var userRepo = new UserRepository(dbContext);
        var tokenRepo = new PasswordResetTokenRepository(dbContext);

        var user = await CreateUserAsync(userRepo);
        var tokenHash = $"hash-{Guid.NewGuid():N}";
        var token = PasswordResetToken.Create(user.Id, tokenHash, DateTime.UtcNow.AddHours(1));
        await tokenRepo.InsertAsync(token);

        var found = await tokenRepo.FindByTokenHashAsync(tokenHash);

        found.Should().NotBeNull();
        found!.TokenHash.Should().Be(tokenHash);
    }

    [Fact]
    public async Task UpdateAsync_MarkAsUsed_PersistsUsedAt()
    {
        await using var dbContext = fixture.CreateDbContext();
        var userRepo = new UserRepository(dbContext);
        var tokenRepo = new PasswordResetTokenRepository(dbContext);

        var user = await CreateUserAsync(userRepo);
        var tokenHash = $"used-{Guid.NewGuid():N}";
        var token = PasswordResetToken.Create(user.Id, tokenHash, DateTime.UtcNow.AddHours(1));
        await tokenRepo.InsertAsync(token);

        token.MarkAsUsed();
        await tokenRepo.UpdateAsync(token);

        var updated = await tokenRepo.FindByTokenHashAsync(tokenHash);
        updated.Should().NotBeNull();
        updated!.IsUsed().Should().BeTrue();
    }

    [Fact]
    public async Task SoftDelete_DeletedToken_IsHiddenByGlobalFilter()
    {
        await using var dbContext = fixture.CreateDbContext();
        var userRepo = new UserRepository(dbContext);
        var tokenRepo = new PasswordResetTokenRepository(dbContext);

        var user = await CreateUserAsync(userRepo);
        var tokenHash = $"soft-{Guid.NewGuid():N}";
        var token = PasswordResetToken.Create(user.Id, tokenHash, DateTime.UtcNow.AddHours(1));
        await tokenRepo.InsertAsync(token);

        token.SoftDelete();
        await tokenRepo.UpdateAsync(token);

        var notFound = await tokenRepo.FindByTokenHashAsync(tokenHash);
        notFound.Should().BeNull("global filter must hide soft-deleted tokens");

        var stillInDb = await dbContext.PasswordResetTokens.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);
        stillInDb.Should().NotBeNull();
        stillInDb!.IsDeleted.Should().BeTrue();
    }

    private static async Task<User> CreateUserAsync(UserRepository userRepo)
    {
        var user = User.Create($"prt-{Guid.NewGuid():N}@test.com", $"prt-user-{Guid.NewGuid():N}", "hash");
        await userRepo.InsertAsync(user);
        return user;
    }
}
