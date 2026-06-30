using FluentAssertions;
using Lumen.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Lumen.IntegrationTests.Persistence;

[Collection(IntegrationCollection.Name)]
[Trait("Category", "Integration")]
public sealed class AuditDbContextIntegrationTests(IntegrationFixture fixture)
{
    [Fact]
    public async Task MigrateAsync_AppliesAllMigrations_WithoutError()
    {
        await using var dbContext = fixture.CreateAuditDbContext();

        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();

        pendingMigrations.Should().BeEmpty();
    }

    [Fact]
    public async Task Database_AfterMigrate_HasAuditEntriesTable()
    {
        await using var dbContext = fixture.CreateAuditDbContext();
        var connection = dbContext.Database.GetDbConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_SCHEMA = 'audit'
              AND TABLE_NAME = 'AuditEntries'
        """;

        var count = Convert.ToInt32(await command.ExecuteScalarAsync());

        count.Should().Be(1, "audit.AuditEntries table must exist after migrations");
    }
}
