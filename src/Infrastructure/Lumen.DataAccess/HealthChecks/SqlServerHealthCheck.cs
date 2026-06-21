using Lumen.DataAccess.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Lumen.DataAccess.HealthChecks;

public sealed class SqlServerHealthCheck : IHealthCheck
{
    private readonly LumenDbContext _dbContext;

    public SqlServerHealthCheck(LumenDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _dbContext.Database.CanConnectAsync(cancellationToken);
            return HealthCheckResult.Healthy("SQL Server is reachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("SQL Server connection failed.", exception: ex);
        }
    }
}
