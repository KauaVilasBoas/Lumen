using Lumen.DataAccess.Persistence;
using Lumen.DataAccess.Persistence.Repositories;
using Lumen.Domain.Users;
using Lumen.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Lumen.IntegrationTests.Persistence;

[Collection(IntegrationCollection.Name)]
[Trait("Category", "Integration")]
public sealed class UserRepositoryIntegrationTests(IntegrationFixture fixture)
{
    [Fact]
    public async Task InsertAsync_ValidUser_PersistsToDatabase()
    {
        await using var dbContext = fixture.CreateDbContext();
        var repository = new UserRepository(dbContext);
        var user = User.Create($"insert-{Guid.NewGuid():N}@test.com", $"user-{Guid.NewGuid():N}", "hash");

        await repository.InsertAsync(user);

        var found = await dbContext.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == user.Id);
        found.Should().NotBeNull();
    }

    [Fact]
    public async Task FindByEmailAsync_ExistingUser_ReturnsUser()
    {
        await using var dbContext = fixture.CreateDbContext();
        var repository = new UserRepository(dbContext);
        var email = $"find-email-{Guid.NewGuid():N}@test.com";
        var user = User.Create(email, $"user-{Guid.NewGuid():N}", "hash");
        await repository.InsertAsync(user);

        var found = await repository.FindByEmailAsync(User.NormalizeEmail(email));

        found.Should().NotBeNull();
        found!.Email.Should().Be(User.NormalizeEmail(email));
    }

    [Fact]
    public async Task FindByUsernameAsync_ExistingUser_ReturnsUser()
    {
        await using var dbContext = fixture.CreateDbContext();
        var repository = new UserRepository(dbContext);
        var username = $"user-{Guid.NewGuid():N}";
        var user = User.Create($"{Guid.NewGuid():N}@test.com", username, "hash");
        await repository.InsertAsync(user);

        var found = await repository.FindByUsernameAsync(username);

        found.Should().NotBeNull();
        found!.Username.Should().Be(username);
    }

    [Fact]
    public async Task InsertAsync_DuplicateEmail_ThrowsDuplicateEmailException()
    {
        await using var dbContext = fixture.CreateDbContext();
        var repository = new UserRepository(dbContext);
        var email = $"dup-email-{Guid.NewGuid():N}@test.com";

        await repository.InsertAsync(User.Create(email, $"user-{Guid.NewGuid():N}", "hash"));

        var act = async () => await repository.InsertAsync(
            User.Create(email, $"user-{Guid.NewGuid():N}", "hash"));

        await act.Should().ThrowAsync<DuplicateEmailException>();
    }

    [Fact]
    public async Task InsertAsync_DuplicateUsername_ThrowsDuplicateUsernameException()
    {
        await using var dbContext = fixture.CreateDbContext();
        var repository = new UserRepository(dbContext);
        var username = $"dup-user-{Guid.NewGuid():N}";

        await repository.InsertAsync(User.Create($"{Guid.NewGuid():N}@test.com", username, "hash"));

        var act = async () => await repository.InsertAsync(
            User.Create($"{Guid.NewGuid():N}@test.com", username, "hash"));

        await act.Should().ThrowAsync<DuplicateUsernameException>();
    }

    [Fact]
    public async Task SoftDelete_DeletedUser_IsHiddenByGlobalFilter()
    {
        await using var dbContext = fixture.CreateDbContext();
        var repository = new UserRepository(dbContext);
        var email = $"soft-del-{Guid.NewGuid():N}@test.com";
        var user = User.Create(email, $"user-{Guid.NewGuid():N}", "hash");
        await repository.InsertAsync(user);

        user.SoftDelete();
        await repository.UpdateAsync(user);

        var notFound = await repository.FindByEmailAsync(User.NormalizeEmail(email));
        notFound.Should().BeNull("global filter must hide soft-deleted users");

        var stillInDb = await dbContext.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == user.Id);
        stillInDb.Should().NotBeNull("soft-deleted row must remain in the database");
        stillInDb!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task SoftDelete_AllowsEmailReuseViaFilteredUniqueIndex()
    {
        await using var dbContext = fixture.CreateDbContext();
        var repository = new UserRepository(dbContext);
        var email = $"reuse-email-{Guid.NewGuid():N}@test.com";
        var original = User.Create(email, $"user-{Guid.NewGuid():N}", "hash");
        await repository.InsertAsync(original);

        original.SoftDelete();
        await repository.UpdateAsync(original);

        var replacement = User.Create(email, $"user-{Guid.NewGuid():N}", "hash");
        var act = async () => await repository.InsertAsync(replacement);

        await act.Should().NotThrowAsync("filtered unique index permits email reuse after soft-delete");
    }

    [Fact]
    public async Task SoftDelete_AllowsUsernameReuseViaFilteredUniqueIndex()
    {
        await using var dbContext = fixture.CreateDbContext();
        var repository = new UserRepository(dbContext);
        var username = $"reuse-user-{Guid.NewGuid():N}";
        var original = User.Create($"{Guid.NewGuid():N}@test.com", username, "hash");
        await repository.InsertAsync(original);

        original.SoftDelete();
        await repository.UpdateAsync(original);

        var replacement = User.Create($"{Guid.NewGuid():N}@test.com", username, "hash");
        var act = async () => await repository.InsertAsync(replacement);

        await act.Should().NotThrowAsync("filtered unique index permits username reuse after soft-delete");
    }
}
