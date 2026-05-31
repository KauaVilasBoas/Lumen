using AegisIdentity.DataAccess.HealthChecks;
using AegisIdentity.Domain.Authorization;
using AegisIdentity.Infrastructure.Configuration;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AegisIdentity.DataAccess.Cache;

public static class RedisServiceExtensions
{
    public static IServiceCollection AddRedisCache(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<RedisOptions>()
            .Bind(configuration.GetSection(RedisOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<RedisCacheOptions>()
            .Configure<IOptions<RedisOptions>>((redisCacheOptions, redisOptions) =>
            {
                redisCacheOptions.Configuration = redisOptions.Value.ConnectionString;
                redisCacheOptions.InstanceName = redisOptions.Value.InstanceName;
            });

        services.AddStackExchangeRedisCache(_ => { });

        services.AddScoped<IUserPermissionCache, UserPermissionCache>();

        return services;
    }

    public static IHealthChecksBuilder AddRedisHealthCheck(this IHealthChecksBuilder builder)
    {
        builder.AddCheck<RedisHealthCheck>("redis");
        return builder;
    }
}
