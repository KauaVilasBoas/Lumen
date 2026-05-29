using AegisIdentity.DataAccess.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;

namespace AegisIdentity.IntegrationTests.Persistence;

public sealed class SqlServerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public AegisIdentityDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AegisIdentityDbContext>()
            .UseSqlServer(
                _container.GetConnectionString(),
                sql => sql.MigrationsAssembly("AegisIdentity.Migrations"))
            .Options;

        return new AegisIdentityDbContext(options);
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var dbContext = CreateDbContext();
        await dbContext.Database.MigrateAsync();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}

[CollectionDefinition(Name)]
public sealed class SqlServerCollection : ICollectionFixture<SqlServerFixture>
{
    public const string Name = "SqlServer";
}
