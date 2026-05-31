// AegisIdentity.Migrations.Cli
//
// Thin wrapper around EF Core migrations for use in CI/CD pipelines or local
// development without the full API host.
//
// Commands:
//   up      — applies all pending EF Core migrations (equivalent to: dotnet ef database update)
//   status  — lists applied and pending migrations
//
// For generating new migrations use the dotnet-ef tooling directly against the
// AegisIdentity.Migrations project:
//   dotnet ef migrations add <MigrationName> \
//     --project src/Migrations/AegisIdentity.Migrations \
//     --startup-project src/Migrations/AegisIdentity.Migrations
//
// The connection string is resolved from:
//   1. Environment variable  SQLSERVER_CONNECTION_STRING
//   2. SqlServer:ConnectionString in appsettings.json (design-time fallback)

using AegisIdentity.DataAccess.Persistence;
using AegisIdentity.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<SqlServerOptions>(
    builder.Configuration.GetSection(SqlServerOptions.SectionName));

builder.Services.AddDbContext<AegisIdentityDbContext>((sp, options) =>
{
    var sqlServerOptions = sp.GetRequiredService<IOptions<SqlServerOptions>>().Value;
    options.UseSqlServer(
        sqlServerOptions.ConnectionString,
        sql => sql.MigrationsAssembly("AegisIdentity.Migrations"));
});

using var host = builder.Build();

using var scope = host.Services.CreateScope();
var dbContext = scope.ServiceProvider.GetRequiredService<AegisIdentityDbContext>();
var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("AegisIdentity.Migrations.Cli");

var command = args.FirstOrDefault()?.ToLowerInvariant() ?? "status";

try
{
    switch (command)
    {
        case "up":
        {
            var pending = (await dbContext.Database.GetPendingMigrationsAsync()).ToList();

            if (pending.Count == 0)
            {
                logger.LogInformation("No pending migrations to apply.");
                return 0;
            }

            logger.LogInformation(
                "Applying {Count} pending migration(s): {Migrations}",
                pending.Count,
                string.Join(", ", pending));

            await dbContext.Database.MigrateAsync();
            logger.LogInformation("All migrations applied.");
            return 0;
        }

        case "status":
        {
            var applied = (await dbContext.Database.GetAppliedMigrationsAsync()).ToList();
            var pending = (await dbContext.Database.GetPendingMigrationsAsync()).ToList();

            Console.WriteLine($"Applied ({applied.Count}):");
            foreach (var m in applied)
                Console.WriteLine($"  + {m}");

            Console.WriteLine($"Pending ({pending.Count}):");
            foreach (var m in pending)
                Console.WriteLine($"  - {m}");

            return 0;
        }

        default:
            Console.Error.WriteLine($"Unknown command '{command}'. Expected: up | status.");
            return 1;
    }
}
catch (Exception ex)
{
    logger.LogError(ex, "Migration command '{Command}' failed.", command);
    return 2;
}
