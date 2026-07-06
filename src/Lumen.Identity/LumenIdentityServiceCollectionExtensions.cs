using System.Net.Http.Headers;
using FluentValidation;
using Lumen.Authorization;
using Lumen.Authorization.Contracts;
using Lumen.Identity.Application.Tokens;
using Lumen.Identity.Domain.Configuration;
using Lumen.Identity.Domain.Notifications;
using Lumen.Identity.Domain.Security;
using Lumen.Identity.Domain.Tokens;
using Lumen.Identity.Domain.Users;
using Lumen.Identity.Infrastructure.Bridges;
using Lumen.Identity.Infrastructure.Configuration;
using Lumen.Identity.Infrastructure.Notifications;
using Lumen.Identity.Infrastructure.Security;
using Lumen.Identity.Persistence;
using Lumen.Identity.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Lumen.Identity;

public static class LumenIdentityServiceCollectionExtensions
{
    private static readonly TimeSpan HibpTimeout = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Registers Lumen.Identity core services: DbContext, repositories, JWT/BCrypt/email
    /// infrastructure, CQRS handlers, and Authorization bridges
    /// (<see cref="IUserDirectory"/>, <see cref="IAuthorizationUserSource"/>).
    /// </summary>
    /// <remarks>
    /// The <paramref name="connectionString"/> must target the same database used by
    /// <c>Lumen.Authorization</c>. Identity uses its own <c>identity</c> schema and DbContext.
    ///
    /// Migrations are registered separately via <c>Lumen.Identity.Migrations</c> (SQL Server)
    /// or <c>Lumen.Identity.Migrations.PostgreSQL</c>. The <see cref="LumenIdentityOptions.Provider"/>
    /// value must match the provider configured in <c>Lumen.Authorization</c>.
    /// </remarks>
    public static IServiceCollection AddLumenIdentityCore(
        this IServiceCollection services,
        string connectionString,
        IConfiguration configuration,
        Action<LumenIdentityOptions>? configure = null)
    {
        var options = new LumenIdentityOptions();
        configure?.Invoke(options);

        RegisterOptions(services, configuration);
        RegisterDbContext(services, connectionString, options.Provider);
        RegisterRepositories(services);
        RegisterSecurity(services);
        RegisterNotifications(services);
        RegisterAuthorizationBridges(services);
        RegisterApplication(services);

        return services;
    }

    /// <summary>
    /// Registers Lumen.Identity core services reading connection string and provider
    /// from <see cref="IConfiguration"/> (<c>ConnectionStrings:DefaultConnection</c> and
    /// <c>Database:Provider</c>).
    /// </summary>
    public static IServiceCollection AddLumenIdentityCore(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<LumenIdentityOptions>? configure = null)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection is required for Lumen.Identity.");

        var options = new LumenIdentityOptions();

        var providerValue = configuration["Database:Provider"];
        if (!string.IsNullOrWhiteSpace(providerValue) &&
            Enum.TryParse<DatabaseProvider>(providerValue, ignoreCase: true, out var parsed))
        {
            options.Provider = parsed;
        }

        configure?.Invoke(options);

        RegisterOptions(services, configuration);
        RegisterDbContext(services, connectionString, options.Provider);
        RegisterRepositories(services);
        RegisterSecurity(services);
        RegisterNotifications(services);
        RegisterAuthorizationBridges(services);
        RegisterApplication(services);

        return services;
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

    private static void RegisterDbContext(
        IServiceCollection services,
        string connectionString,
        DatabaseProvider provider)
    {
        services.AddDbContext<IdentityDbContext>(dbOptions =>
        {
            switch (provider)
            {
                case DatabaseProvider.SqlServer:
                    dbOptions.UseSqlServer(
                        connectionString,
                        sql => sql.MigrationsAssembly(IdentityMigrationsAssemblyNames.SqlServer));
                    break;

                case DatabaseProvider.PostgreSQL:
                    dbOptions.UseNpgsql(
                        connectionString,
                        npgsql => npgsql.MigrationsAssembly(IdentityMigrationsAssemblyNames.PostgreSQL));
                    break;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(provider),
                        provider,
                        "Unsupported database provider for Lumen.Identity.");
            }
        });
    }

    private static void RegisterRepositories(IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
        services.AddScoped<IEmailConfirmationTokenRepository, EmailConfirmationTokenRepository>();
    }

    private static void RegisterSecurity(IServiceCollection services)
    {
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<IJwtService, JwtService>();
        services.AddScoped<IAppSettings, AppSettingsAdapter>();
    }

    private static void RegisterNotifications(IServiceCollection services)
    {
        services.AddSingleton<IEmailTemplateRenderer, EmailTemplateRenderer>();
        services.AddScoped<ISmtpTransport, MailKitSmtpTransport>();
        services.AddScoped<IEmailService, MailKitEmailService>();
        services.AddScoped<IEmailConfirmationService, EmailConfirmationService>();

        services.AddMemoryCache();
        services.AddHttpClient<IPwnedPasswordsClient, PwnedPasswordsClient>((sp, client) =>
        {
            var hibpOptions = sp.GetRequiredService<IOptions<IdentityHibpOptions>>().Value;
            client.BaseAddress = new Uri(EnsureTrailingSlash(hibpOptions.ApiBaseUrl));
            client.Timeout = HibpTimeout;
            client.DefaultRequestHeaders.UserAgent.Clear();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(hibpOptions.UserAgent);
            client.DefaultRequestHeaders.Add("Add-Padding", "true");
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
        });

        services.AddScoped<IPwnedPasswordsClient, PwnedPasswordsClient>();
        services.AddScoped<IPasswordValidator, PasswordValidator>();
    }

    private static void RegisterAuthorizationBridges(IServiceCollection services)
    {
        // Replace the no-op stubs registered by Lumen.Authorization with Identity-backed impls.
        services.Replace(ServiceDescriptor.Scoped<IUserDirectory, IdentityUserDirectory>());
        services.Replace(ServiceDescriptor.Scoped<IAuthorizationUserSource, IdentityAuthorizationUserSource>());
    }

    private static void RegisterApplication(IServiceCollection services)
    {
        var assembly = typeof(LumenIdentityServiceCollectionExtensions).Assembly;

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
        });

        services.AddScoped<ITokenCleanupService, TokenCleanupService>();

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
