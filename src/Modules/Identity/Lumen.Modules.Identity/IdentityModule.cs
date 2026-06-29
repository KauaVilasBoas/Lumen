using System.Net.Http.Headers;
using FluentValidation;
using Lumen.Modularity;
using Lumen.Modules.Identity.Application.Behaviors;
using Lumen.Modules.Identity.Domain.Authorization;
using Lumen.Modules.Identity.Domain.Configuration;
using Lumen.Modules.Identity.Domain.Notifications;
using Lumen.Modules.Identity.Domain.Security;
using Lumen.Modules.Identity.Domain.Tokens;
using Lumen.Modules.Identity.Domain.Users;
using Lumen.Modules.Identity.Infrastructure.Cache;
using Lumen.Modules.Identity.Infrastructure.Configuration;
using Lumen.Modules.Identity.Infrastructure.Notifications;
using Lumen.Modules.Identity.Infrastructure.Security;
using Lumen.Modules.Identity.Persistence;
using Lumen.Modules.Identity.Persistence.Repositories;
using Microsoft.AspNetCore.Routing;
using IdentityDomainPermissionService = Lumen.Modules.Identity.Domain.Authorization.IUserPermissionService;
using IdentityContractsPermissionService = Lumen.Modules.Identity.Contracts.IUserPermissionService;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lumen.Modules.Identity;

[Module]
public sealed class IdentityModule : IModule
{
    private static readonly TimeSpan HibpTimeout = TimeSpan.FromSeconds(2);

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        RegisterOptions(services, configuration);
        RegisterDbContext(services, configuration);
        RegisterRepositories(services);
        RegisterSecurity(services);
        RegisterNotifications(services, configuration);
        RegisterCache(services, configuration);
        RegisterApplication(services);
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
    }

    private static void RegisterOptions(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<IdentityJwtOptions>()
            .Bind(configuration.GetSection(IdentityJwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<IdentityAppOptions>()
            .Bind(configuration.GetSection(IdentityAppOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<IdentitySmtpOptions>()
            .Bind(configuration.GetSection(IdentitySmtpOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<IdentityHibpOptions>()
            .Bind(configuration.GetSection(IdentityHibpOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }

    private static void RegisterDbContext(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<IdentityDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsAssembly(IdentityMigrationsAssembly.Name)));
    }

    private static void RegisterRepositories(IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
        services.AddScoped<IEmailConfirmationTokenRepository, EmailConfirmationTokenRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IGroupPermissionRepository, GroupPermissionRepository>();
        services.AddScoped<IProfileRepository, ProfileRepository>();
        services.AddScoped<IUserProfileRepository, UserProfileRepository>();
    }

    private static void RegisterSecurity(IServiceCollection services)
    {
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<IJwtService, JwtService>();
        services.AddScoped<IPwnedPasswordsClient, PwnedPasswordsClient>();
        services.AddScoped<IPasswordValidator, PasswordValidator>();
        services.AddScoped<IAppSettings, AppSettingsAdapter>();
    }

    private static void RegisterNotifications(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IEmailTemplateRenderer, EmailTemplateRenderer>();
        services.AddScoped<ISmtpTransport, MailKitSmtpTransport>();
        services.AddScoped<IEmailService, MailKitEmailService>();
        services.AddScoped<IEmailConfirmationService, EmailConfirmationService>();

        services.AddMemoryCache();
        services
            .AddHttpClient<IPwnedPasswordsClient, PwnedPasswordsClient>((sp, client) =>
            {
                var options = sp.GetRequiredService<IOptions<IdentityHibpOptions>>().Value;
                client.BaseAddress = new Uri(EnsureTrailingSlash(options.ApiBaseUrl));
                client.Timeout = HibpTimeout;
                client.DefaultRequestHeaders.UserAgent.Clear();
                client.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);
                client.DefaultRequestHeaders.Add("Add-Padding", "true");
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
            });
    }

    private static void RegisterCache(IServiceCollection services, IConfiguration configuration)
    {
        services.AddStackExchangeRedisCache(options =>
            options.Configuration = configuration.GetConnectionString("Redis"));

        services.AddScoped<IUserPermissionCache, UserPermissionCache>();
        services.AddScoped<UserPermissionService>();
        services.AddScoped<IdentityDomainPermissionService>(sp => sp.GetRequiredService<UserPermissionService>());
        services.AddScoped<IdentityContractsPermissionService>(sp => sp.GetRequiredService<UserPermissionService>());
    }

    private static void RegisterApplication(IServiceCollection services)
    {
        var assembly = typeof(IdentityModule).Assembly;

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        RegisterValidators(services, assembly);
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

    private static string EnsureTrailingSlash(string baseUrl)
        => baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/";
}
