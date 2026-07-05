using Lumen.Authorization.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Lumen.Authorization.Migrations.PostgreSQL;

/// <summary>
/// Factory usada pelo CLI do EF Core (<c>dotnet ef migrations add</c>) para
/// criar o <see cref="LumenAuthorizationDbContext"/> configurado para PostgreSQL.
/// </summary>
/// <remarks>
/// Prioridade de resolução da connection string:
/// <list type="number">
///   <item><c>appsettings.json</c> → <c>ConnectionStrings:DefaultConnection</c></item>
///   <item>Variável de ambiente <c>POSTGRES_CONNECTION_STRING</c></item>
///   <item>String embutida de fallback (local dev)</item>
/// </list>
/// </remarks>
internal sealed class LumenAuthorizationPostgresDbContextFactory
    : IDesignTimeDbContextFactory<LumenAuthorizationDbContext>
{
    public LumenAuthorizationDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING")
            ?? "Host=localhost;Database=lumen;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<LumenAuthorizationDbContext>();
        optionsBuilder.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsAssembly(typeof(LumenAuthorizationPostgresDbContextFactory).Assembly.FullName));

        return new LumenAuthorizationDbContext(optionsBuilder.Options);
    }
}
