using Lumen.Authorization.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Lumen.Authorization.Migrations;

internal sealed class LumenAuthorizationDbContextFactory : IDesignTimeDbContextFactory<LumenAuthorizationDbContext>
{
    public LumenAuthorizationDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? Environment.GetEnvironmentVariable("SQLSERVER_CONNECTION_STRING")
            ?? "Server=localhost;Database=Lumen;Trusted_Connection=True;TrustServerCertificate=True;";

        var optionsBuilder = new DbContextOptionsBuilder<LumenAuthorizationDbContext>();
        optionsBuilder.UseSqlServer(
            connectionString,
            sql => sql.MigrationsAssembly(typeof(LumenAuthorizationDbContextFactory).Assembly.FullName));

        return new LumenAuthorizationDbContext(optionsBuilder.Options);
    }
}
