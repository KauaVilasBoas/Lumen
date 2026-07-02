using FluentValidation;
using Lumen.Authorization.Application;
using Lumen.Authorization.Application.Behaviors;
using Lumen.Authorization.Application.Permissions;
using Lumen.Authorization.Contracts;
using Lumen.Authorization.Domain;
using Lumen.Authorization.Infrastructure;
using Lumen.Authorization.Infrastructure.Cache;
using Lumen.Authorization.Persistence;
using Lumen.Authorization.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Lumen.Authorization;

public static class LumenAuthorizationServiceCollectionExtensions
{
    public static IServiceCollection AddLumenAuthorization(
        this IServiceCollection services,
        string connectionString,
        Action<LumenAuthorizationOptions>? configure = null)
    {
        var options = new LumenAuthorizationOptions();
        configure?.Invoke(options);

        return services.RegisterCore(connectionString, options);
    }

    public static IServiceCollection AddLumenAuthorization(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<LumenAuthorizationOptions>? configure = null)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        var redisConnectionString = configuration.GetConnectionString("Redis");

        var options = new LumenAuthorizationOptions
        {
            RedisConnectionString = redisConnectionString
        };

        configure?.Invoke(options);

        return services.RegisterCore(connectionString!, options);
    }

    private static IServiceCollection RegisterCore(
        this IServiceCollection services,
        string connectionString,
        LumenAuthorizationOptions options)
    {
        var assembly = typeof(LumenAuthorizationServiceCollectionExtensions).Assembly;

        services.Configure<LumenAuthorizationOptions>(o =>
        {
            o.RedisConnectionString = options.RedisConnectionString;
            o.ApplyMigrationsOnStartup = options.ApplyMigrationsOnStartup;
            o.UserIdClaimType = options.UserIdClaimType;
        });

        services.AddDbContext<LumenAuthorizationDbContext>(dbOptions =>
            dbOptions.UseSqlServer(
                connectionString,
                sql => sql.MigrationsAssembly(LumenAuthorizationMigrationsAssembly.Name)));

        RegisterCacheProvider(services, options);

        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IGroupPermissionRepository, GroupPermissionRepository>();
        services.AddScoped<IProfileRepository, ProfileRepository>();
        services.AddScoped<IUserProfileRepository, UserProfileRepository>();

        services.AddScoped<IUserPermissionCache, UserPermissionCache>();
        services.AddScoped<UserPermissionService>();
        services.AddScoped<Domain.IUserPermissionService>(sp => sp.GetRequiredService<UserPermissionService>());
        services.AddScoped<Contracts.IUserPermissionService>(sp => sp.GetRequiredService<UserPermissionService>());

        services.AddScoped<IPermissionSyncService, PermissionSyncService>();
        services.AddScoped<IUserProfileGuard, UserProfileGuard>();

        services.TryAddScoped<IUserDirectory, NoOpUserDirectory>();
        services.TryAddScoped<IAuthorizationUserSource, EmptyAuthorizationUserSource>();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        RegisterValidators(services, assembly);

        return services;
    }

    private static void RegisterCacheProvider(IServiceCollection services, LumenAuthorizationOptions options)
    {
        if (services.Any(d => d.ServiceType == typeof(IDistributedCache)))
            return;

        if (!string.IsNullOrWhiteSpace(options.RedisConnectionString))
        {
            services.AddStackExchangeRedisCache(o => o.Configuration = options.RedisConnectionString);
            return;
        }

        services.AddDistributedMemoryCache();
    }

    private static void RegisterValidators(IServiceCollection services, System.Reflection.Assembly assembly)
    {
        var validatorType = typeof(IValidator<>);

        var validators = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == validatorType)
                .Select(i => (Implementation: t, Interface: i)));

        foreach (var (implementation, @interface) in validators)
            services.AddScoped(@interface, implementation);
    }
}
