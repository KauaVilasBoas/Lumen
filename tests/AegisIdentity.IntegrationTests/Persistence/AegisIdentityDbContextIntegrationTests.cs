using AegisIdentity.DataAccess.Persistence;
using AegisIdentity.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AegisIdentity.IntegrationTests.Persistence;

[Collection(IntegrationCollection.Name)]
[Trait("Category", "Integration")]
public sealed class AegisIdentityDbContextIntegrationTests(IntegrationFixture fixture)
{
    [Fact]
    public async Task MigrateAsync_AppliesAllMigrations_WithoutError()
    {
        await using var dbContext = fixture.CreateDbContext();

        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();

        pendingMigrations.Should().BeEmpty();
    }

    [Fact]
    public async Task Database_AfterMigrate_HasExpectedTables()
    {
        await using var dbContext = fixture.CreateDbContext();
        var connection = dbContext.Database.GetDbConnection();

        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_NAME IN ('Users', 'RefreshTokens', 'PasswordResetTokens', 'EmailConfirmationTokens')
        """;

        var count = Convert.ToInt32(await command.ExecuteScalarAsync());

        count.Should().Be(4);
    }

    [Fact]
    public async Task Users_EmailFilteredUniqueIndex_ExistsWithCorrectDefinition()
    {
        await using var dbContext = fixture.CreateDbContext();
        var connection = dbContext.Database.GetDbConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT i.is_unique, i.filter_definition
            FROM sys.indexes i
            INNER JOIN sys.objects o ON i.object_id = o.object_id
            WHERE o.name = 'Users'
              AND i.name = 'ix_users_email_unique'
        """;

        await using var reader = await command.ExecuteReaderAsync();
        reader.HasRows.Should().BeTrue("filtered unique index ix_users_email_unique must exist on Users");

        await reader.ReadAsync();
        bool isUnique = reader.GetBoolean(0);
        string filterDefinition = reader.GetString(1);

        isUnique.Should().BeTrue();
        filterDefinition.Should().Contain("IsDeleted");
    }

    [Fact]
    public async Task Users_UsernameFilteredUniqueIndex_ExistsWithCorrectDefinition()
    {
        await using var dbContext = fixture.CreateDbContext();
        var connection = dbContext.Database.GetDbConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT i.is_unique, i.filter_definition
            FROM sys.indexes i
            INNER JOIN sys.objects o ON i.object_id = o.object_id
            WHERE o.name = 'Users'
              AND i.name = 'ix_users_username_unique'
        """;

        await using var reader = await command.ExecuteReaderAsync();
        reader.HasRows.Should().BeTrue("filtered unique index ix_users_username_unique must exist on Users");

        await reader.ReadAsync();
        bool isUnique = reader.GetBoolean(0);
        string filterDefinition = reader.GetString(1);

        isUnique.Should().BeTrue();
        filterDefinition.Should().Contain("IsDeleted");
    }

    [Fact]
    public async Task Users_LockedUntilFilteredIndex_ExistsWithNullFilter()
    {
        await using var dbContext = fixture.CreateDbContext();
        var connection = dbContext.Database.GetDbConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT i.filter_definition
            FROM sys.indexes i
            INNER JOIN sys.objects o ON i.object_id = o.object_id
            WHERE o.name = 'Users'
              AND i.name = 'ix_users_locked_until'
        """;

        await using var reader = await command.ExecuteReaderAsync();
        reader.HasRows.Should().BeTrue("filtered index ix_users_locked_until must exist on Users");

        await reader.ReadAsync();
        string filterDefinition = reader.GetString(0);

        filterDefinition.Should().Contain("LockedUntil");
    }
}
