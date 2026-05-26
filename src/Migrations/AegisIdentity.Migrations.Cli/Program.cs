using AegisIdentity.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

var builder = Host.CreateApplicationBuilder(args);

var connectionString = builder.Configuration["Mongo:ConnectionString"]
    ?? throw new InvalidOperationException("Configuration value 'Mongo:ConnectionString' is required.");
var databaseName = builder.Configuration["Mongo:Database"]
    ?? throw new InvalidOperationException("Configuration value 'Mongo:Database' is required.");

builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(connectionString));
builder.Services.AddSingleton<IMongoDatabase>(sp =>
    sp.GetRequiredService<IMongoClient>().GetDatabase(databaseName));

builder.Services.AddMongoMigrations();

using var host = builder.Build();

// Runner is registered Scoped to compose with Api wiring; resolve it through
// a scope here too so the CLI uses the same lifetime rules as the Api.
using var scope = host.Services.CreateScope();
var runner = scope.ServiceProvider.GetRequiredService<MongoMigrationRunner>();
var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("AegisIdentity.Migrations.Cli");

var command = args.FirstOrDefault()?.ToLowerInvariant() ?? "status";

try
{
    switch (command)
    {
        case "up":
        {
            var applied = await runner.ApplyPendingAsync(CancellationToken.None);
            logger.LogInformation("Applied {Count} pending migration(s).", applied);
            return 0;
        }
        case "down":
        {
            var reverted = await runner.RevertLastAsync(CancellationToken.None);
            logger.LogInformation(reverted ? "Reverted last migration." : "Nothing to revert.");
            return 0;
        }
        case "status":
        {
            var status = await runner.GetStatusAsync(CancellationToken.None);
            Console.WriteLine($"Applied ({status.Applied.Count}):");
            foreach (var m in status.Applied)
                Console.WriteLine($"  + {m.Id}  {m.Name}");
            Console.WriteLine($"Pending ({status.Pending.Count}):");
            foreach (var m in status.Pending)
                Console.WriteLine($"  - {m.Id}  {m.Name}");
            return 0;
        }
        default:
            Console.Error.WriteLine($"Unknown command '{command}'. Expected: up | down | status.");
            return 1;
    }
}
catch (Exception ex)
{
    logger.LogError(ex, "Migration command '{Command}' failed.", command);
    return 2;
}
