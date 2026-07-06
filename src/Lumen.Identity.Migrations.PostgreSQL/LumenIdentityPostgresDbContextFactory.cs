using Lumen.Identity.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Lumen.Identity.Migrations.PostgreSQL;

/// <summary>
/// Factory used by the EF Core CLI (<c>dotnet ef migrations add</c>) to create
/// <see cref="IdentityDbContext"/> configured for PostgreSQL.
/// </summary>
internal sealed class LumenIdentityPostgresDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING")
            ?? "Host=localhost;Database=lumen;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<IdentityDbContext>();
        optionsBuilder.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsAssembly(typeof(LumenIdentityPostgresDbContextFactory).Assembly.FullName));

        return new IdentityDbContext(optionsBuilder.Options);
    }
}
