using AegisIdentity.Infrastructure.Configuration;
using AegisIdentity.Jobs.Contracts;
using AegisIdentity.Jobs.Dashboard;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AegisIdentity.Jobs.Configuration;

public static class HangfireServiceCollectionExtensions
{
    public static IServiceCollection AddAegisHangfire(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHangfire((serviceProvider, config) =>
        {
            var sqlServerOptions = serviceProvider
                .GetRequiredService<IOptions<SqlServerOptions>>()
                .Value;

            var storageOptions = new SqlServerStorageOptions
            {
                SchemaName = "HangFire",
                PrepareSchemaIfNecessary = true,
            };

            config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSqlServerStorage(sqlServerOptions.ConnectionString, storageOptions);
        });

        return services;
    }

    public static IServiceCollection AddAegisHangfireServer(
        this IServiceCollection services,
        Action<BackgroundJobServerOptions>? configure = null)
    {
        services.AddHangfireServer(options => configure?.Invoke(options));
        return services;
    }

    public static IServiceCollection AddAegisDashboard(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<HangfireDashboardOptions>(
            configuration.GetSection(HangfireDashboardOptions.SectionName));

        services.AddTransient<HangfireDashboardAuthorizationFilter>();

        return services;
    }

    public static IServiceCollection RegisterJobs(this IServiceCollection services)
    {
        var jobTypes = typeof(IJobDefinition).Assembly
            .GetTypes()
            .Where(t => typeof(IJobDefinition).IsAssignableFrom(t)
                        && t is { IsAbstract: false, IsInterface: false });

        foreach (var type in jobTypes)
        {
            services.AddTransient(type);
            services.AddTransient(typeof(IJobDefinition), type);
        }

        return services;
    }
}
