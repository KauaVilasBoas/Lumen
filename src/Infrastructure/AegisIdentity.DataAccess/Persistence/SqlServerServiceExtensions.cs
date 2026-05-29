using AegisIdentity.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AegisIdentity.DataAccess.Persistence;

public static class SqlServerServiceExtensions
{
    public static IServiceCollection AddRelationalDataAccess(this IServiceCollection services)
    {
        services.AddDbContext<AegisIdentityDbContext>((serviceProvider, options) =>
        {
            var sqlServerOptions = serviceProvider
                .GetRequiredService<IOptions<SqlServerOptions>>()
                .Value;

            options.UseSqlServer(sqlServerOptions.ConnectionString);
        });

        return services;
    }
}
