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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lumen.Authorization;

public static class LumenAuthorizationServiceCollectionExtensions
{
    public static IServiceCollection AddLumenAuthorization(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var assembly = typeof(LumenAuthorizationServiceCollectionExtensions).Assembly;

        services.AddDbContext<LumenAuthorizationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

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
