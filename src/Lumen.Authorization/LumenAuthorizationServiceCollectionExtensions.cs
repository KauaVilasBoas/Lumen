using FluentValidation;
using Lumen.Authorization.Application.Behaviors;
using Lumen.Authorization.Contracts;
using Lumen.Authorization.Domain;
using Lumen.Authorization.Infrastructure;
using Lumen.Authorization.Infrastructure.Cache;
using Lumen.Authorization.Internal;
using Lumen.Authorization.Persistence;
using Lumen.Authorization.Persistence.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;

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
        var providerValue = configuration["Database:Provider"];

        var options = new LumenAuthorizationOptions
        {
            RedisConnectionString = redisConnectionString
        };

        if (!string.IsNullOrWhiteSpace(providerValue) &&
            Enum.TryParse<DatabaseProvider>(providerValue, ignoreCase: true, out var parsed))
        {
            options.Provider = parsed;
        }

        configure?.Invoke(options);

        return services.RegisterCore(connectionString!, options);
    }

    private static IServiceCollection RegisterCore(
        this IServiceCollection services,
        string connectionString,
        LumenAuthorizationOptions options)
    {
        ValidateConnectionString(connectionString, options.Provider);

        var assembly = typeof(LumenAuthorizationServiceCollectionExtensions).Assembly;

        services.Configure<LumenAuthorizationOptions>(o =>
        {
            o.Provider = options.Provider;
            o.RedisConnectionString = options.RedisConnectionString;
            o.ApplyMigrationsOnStartup = options.ApplyMigrationsOnStartup;
            o.UserIdClaimType = options.UserIdClaimType;
        });

        RegisterDbContext(services, connectionString, options.Provider);
        RegisterCacheProvider(services, options);

        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IGroupPermissionRepository, GroupPermissionRepository>();
        services.AddScoped<IProfileRepository, ProfileRepository>();
        services.AddScoped<IUserProfileRepository, UserProfileRepository>();

        services.AddScoped<IUserPermissionCache, UserPermissionCache>();
        services.AddScoped<UserPermissionService>();
        services.AddScoped<Domain.IUserPermissionService>(sp => sp.GetRequiredService<UserPermissionService>());
        services.AddScoped<Contracts.IUserPermissionService>(sp => sp.GetRequiredService<UserPermissionService>());

        services.TryAddScoped<IUserDirectory, NoOpUserDirectory>();
        services.TryAddScoped<IAuthorizationUserSource, EmptyAuthorizationUserSource>();
        services.TryAddScoped<ITenantScopeAccessor, NoOpTenantScopeAccessor>();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        RegisterValidators(services, assembly);

        return services;
    }

    private static void RegisterDbContext(
        IServiceCollection services,
        string connectionString,
        DatabaseProvider provider)
    {
        services.AddDbContext<LumenAuthorizationDbContext>(dbOptions =>
        {
            switch (provider)
            {
                case DatabaseProvider.SqlServer:
                    dbOptions.UseSqlServer(
                        connectionString,
                        sql => sql.MigrationsAssembly(LumenAuthorizationMigrationsAssembly.SqlServer));
                    break;

                case DatabaseProvider.PostgreSQL:
                    dbOptions.UseNpgsql(
                        connectionString,
                        npgsql => npgsql.MigrationsAssembly(LumenAuthorizationMigrationsAssembly.PostgreSQL));
                    break;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(provider),
                        provider,
                        AuthorizationErrorMessages.UnknownProvider);
            }
        });
    }

    private static void ValidateConnectionString(string connectionString, DatabaseProvider provider)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException(AuthorizationErrorMessages.ConnectionStringNullOrEmpty, nameof(connectionString));

        switch (provider)
        {
            case DatabaseProvider.SqlServer:
                ValidateSqlServerConnectionString(connectionString);
                break;

            case DatabaseProvider.PostgreSQL:
                ValidatePostgresConnectionString(connectionString);
                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(provider),
                    provider,
                    AuthorizationErrorMessages.UnknownProvider);
        }
    }

    private static void ValidateSqlServerConnectionString(string connectionString)
    {
        try
        {
            _ = new SqlConnectionStringBuilder(connectionString);
        }
        catch (Exception ex)
        {
            throw new ArgumentException(AuthorizationErrorMessages.ConnectionStringNotSqlServer, nameof(connectionString), ex);
        }
    }

    private static void ValidatePostgresConnectionString(string connectionString)
    {
        try
        {
            _ = new NpgsqlConnectionStringBuilder(connectionString);
        }
        catch (Exception ex)
        {
            throw new ArgumentException(AuthorizationErrorMessages.ConnectionStringNotPostgres, nameof(connectionString), ex);
        }
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
