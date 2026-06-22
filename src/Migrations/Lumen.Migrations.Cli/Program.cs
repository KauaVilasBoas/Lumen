// Lumen.Migrations.Cli
//
// Thin wrapper around EF Core migrations for use in CI/CD pipelines or local
// development without the full API host.
//
// Commands:
//   up      — (default) prints migration status, applies all pending EF Core
//             migrations, then prints the status again. Running with no command
//             is equivalent to 'up'.
//   status  — lists applied and pending migrations without touching the
//             database (read-only).
//
// For generating new migrations use the dotnet-ef tooling directly against the
// Lumen.Migrations project:
//   dotnet ef migrations add <MigrationName> \
//     --project src/Migrations/Lumen.Migrations \
//     --startup-project src/Migrations/Lumen.Migrations
//
// The connection string is resolved from:
//   1. Environment variable  SqlServer__ConnectionString
//   2. SqlServer:ConnectionString in appsettings.json (design-time fallback)

using Lumen.DataAccess.Persistence;
using Lumen.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

// Anchor the content root to the binary's directory (rather than the current
// working directory) so appsettings.json is found regardless of where the CLI
// is launched from — e.g. `dotnet run` from the repository root vs. the IDE
// launching from the output directory.
var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory,
});

builder.Services.Configure<SqlServerOptions>(
    builder.Configuration.GetSection(SqlServerOptions.SectionName));

builder.Services.AddDbContext<LumenDbContext>((sp, options) =>
{
    var sqlServerOptions = sp.GetRequiredService<IOptions<SqlServerOptions>>().Value;
    options.UseSqlServer(
        sqlServerOptions.ConnectionString,
        sql => sql.MigrationsAssembly("Lumen.Migrations"));
});

using var host = builder.Build();

using var scope = host.Services.CreateScope();
var dbContext = scope.ServiceProvider.GetRequiredService<LumenDbContext>();
var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Lumen.Migrations.Cli");

// Default to 'up': running the CLI with no arguments always applies pending
// migrations (listing the status before and after).
var command = args.FirstOrDefault()?.ToLowerInvariant() ?? "up";

async Task PrintStatusAsync(string label)
{
    var applied = (await dbContext.Database.GetAppliedMigrationsAsync()).ToList();
    var pending = (await dbContext.Database.GetPendingMigrationsAsync()).ToList();

    Console.WriteLine($"--- {label} ---");
    Console.WriteLine($"Applied ({applied.Count}):");
    foreach (var m in applied)
        Console.WriteLine($"  + {m}");

    Console.WriteLine($"Pending ({pending.Count}):");
    foreach (var m in pending)
        Console.WriteLine($"  - {m}");
}

try
{
    switch (command)
    {
        case "up":
        {
            await PrintStatusAsync("status before 'up'");

            var pending = (await dbContext.Database.GetPendingMigrationsAsync()).ToList();

            if (pending.Count == 0)
            {
                logger.LogInformation("No pending migrations to apply.");
            }
            else
            {
                logger.LogInformation(
                    "Applying {Count} pending migration(s): {Migrations}",
                    pending.Count,
                    string.Join(", ", pending));

                await dbContext.Database.MigrateAsync();
                logger.LogInformation("All migrations applied.");
            }

            await PrintStatusAsync("status after 'up'");
            return 0;
        }

        case "status":
        {
            await PrintStatusAsync("status");
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
