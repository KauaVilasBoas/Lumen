using FluentAssertions;
using Lumen.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Lumen.IntegrationTests.Persistence;

[Collection(IntegrationCollection.Name)]
[Trait("Category", "Integration")]
public sealed class IdentityDbContextIntegrationTests(IntegrationFixture fixture)
{
    [Fact]
    public async Task MigrateAsync_AppliesAllMigrations_WithoutError()
    {
        await using var dbContext = fixture.CreateIdentityDbContext();

        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();

        pendingMigrations.Should().BeEmpty();
    }

    [Fact]
    public async Task Database_AfterMigrate_HasExpectedIdentityTables()
    {
        await using var dbContext = fixture.CreateIdentityDbContext();
        var connection = dbContext.Database.GetDbConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_SCHEMA = 'identity'
              AND TABLE_NAME IN (
                'Users', 'RefreshTokens', 'PasswordResetTokens',
                'EmailConfirmationTokens', 'Permissions', 'Profiles'
              )
        """;

        var count = Convert.ToInt32(await command.ExecuteScalarAsync());

        count.Should().Be(6, "identity.* schema must have all six core tables after migrations");
    }

    [Fact]
    public async Task Users_EmailFilteredUniqueIndex_ExistsWithCorrectDefinition()
    {
        await using var dbContext = fixture.CreateIdentityDbContext();
        var connection = dbContext.Database.GetDbConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT i.is_unique, i.filter_definition
            FROM sys.indexes i
            INNER JOIN sys.objects o ON i.object_id = o.object_id
            WHERE o.name = 'Users'
              AND i.name = 'ix_identity_users_email_unique'
        """;

        await using var reader = await command.ExecuteReaderAsync();
        reader.HasRows.Should().BeTrue("filtered unique index ix_identity_users_email_unique must exist on identity.Users");

        await reader.ReadAsync();
        bool isUnique = reader.GetBoolean(0);
        string filterDefinition = reader.GetString(1);

        isUnique.Should().BeTrue();
        filterDefinition.Should().Contain("IsDeleted");
    }

    [Fact]
    public async Task Users_UsernameFilteredUniqueIndex_ExistsWithCorrectDefinition()
    {
        await using var dbContext = fixture.CreateIdentityDbContext();
        var connection = dbContext.Database.GetDbConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT i.is_unique, i.filter_definition
            FROM sys.indexes i
            INNER JOIN sys.objects o ON i.object_id = o.object_id
            WHERE o.name = 'Users'
              AND i.name = 'ix_identity_users_username_unique'
        """;

        await using var reader = await command.ExecuteReaderAsync();
        reader.HasRows.Should().BeTrue("filtered unique index ix_identity_users_username_unique must exist on identity.Users");

        await reader.ReadAsync();
        bool isUnique = reader.GetBoolean(0);
        string filterDefinition = reader.GetString(1);

        isUnique.Should().BeTrue();
        filterDefinition.Should().Contain("IsDeleted");
    }
}
