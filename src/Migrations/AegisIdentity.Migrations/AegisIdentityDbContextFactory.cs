using AegisIdentity.DataAccess.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace AegisIdentity.Migrations;

public sealed class AegisIdentityDbContextFactory : IDesignTimeDbContextFactory<AegisIdentityDbContext>
{
    public AegisIdentityDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration["SqlServer:ConnectionString"]
            ?? Environment.GetEnvironmentVariable("SQLSERVER_CONNECTION_STRING")
            ?? "Server=localhost;Database=AegisIdentity;Trusted_Connection=True;TrustServerCertificate=True;";

        var optionsBuilder = new DbContextOptionsBuilder<AegisIdentityDbContext>();
        optionsBuilder.UseSqlServer(
            connectionString,
            sql => sql.MigrationsAssembly(typeof(AegisIdentityDbContextFactory).Assembly.FullName));

        return new AegisIdentityDbContext(optionsBuilder.Options);
    }
}
