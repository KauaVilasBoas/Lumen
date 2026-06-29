using Lumen.Modularity;
using Lumen.Modules.Audit.Persistence;
using Lumen.SharedKernel.Constants;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lumen.Modules.Audit;

[Module]
public sealed class AuditModule : IModule
{
    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AuditDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsAssembly(AuditMigrationsAssembly.Name)));

        services.AddScoped<AuditRepository>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
    }
}
