using Lumen.Authorization.Contracts;
using Lumen.Authorization.Migrations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using CoreExtensions = Lumen.Authorization.LumenAuthorizationServiceCollectionExtensions;

namespace Lumen.Authorization.AspNetCore;

public static class LumenAuthorizationAspNetCoreServiceCollectionExtensions
{
    /// <summary>
    /// Composição em uma chamada: registra o núcleo de autorização Lumen, as migrations de
    /// startup, o enforcement de permissões e a discovery/reconciliation de permissões.
    /// </summary>
    /// <remarks>
    /// Equivalente a chamar na sequência:
    /// <c>AddLumenAuthorization(connectionString, configure)</c> (namespace <c>Lumen.Authorization</c>),
    /// <c>AddLumenAuthorizationMigrations()</c>, <c>AddLumenAuthorizationEnforcement()</c> e
    /// <c>AddLumenAuthorizationDiscovery()</c>.
    ///
    /// Use este overload em hosts ASP.NET Core. O overload de mesmo nome em
    /// <c>Lumen.Authorization</c> (namespace distinto) registra apenas o núcleo — indicado para
    /// módulos ou libs que não hospedam middleware ASP.NET Core diretamente (ex.: módulos DDD).
    /// </remarks>
    /// <param name="services">A coleção de serviços.</param>
    /// <param name="connectionString">Connection string SQL Server não vazia.</param>
    /// <param name="configure">Configuração opcional de <see cref="LumenAuthorizationOptions"/>.</param>
    public static IServiceCollection AddLumenAuthorization(
        this IServiceCollection services,
        string connectionString,
        Action<LumenAuthorizationOptions>? configure = null)
    {
        CoreExtensions.AddLumenAuthorization(services, connectionString, configure);
        services.AddLumenAuthorizationMigrations();
        services.AddLumenAuthorizationEnforcement();
        services.AddLumenAuthorizationDiscovery();

        return services;
    }

    /// <summary>
    /// Composição em uma chamada lendo a configuração de <see cref="IConfiguration"/>: registra
    /// o núcleo de autorização Lumen, as migrations de startup, o enforcement de permissões e a
    /// discovery/reconciliation de permissões.
    /// </summary>
    /// <remarks>
    /// A connection string SQL Server é lida de <c>ConnectionStrings:DefaultConnection</c>.
    /// A connection string Redis (opcional) é lida de <c>ConnectionStrings:Redis</c>.
    ///
    /// Equivalente a chamar na sequência:
    /// <c>AddLumenAuthorization(configuration, configure)</c> (namespace <c>Lumen.Authorization</c>),
    /// <c>AddLumenAuthorizationMigrations()</c>, <c>AddLumenAuthorizationEnforcement()</c> e
    /// <c>AddLumenAuthorizationDiscovery()</c>.
    ///
    /// Use este overload em hosts ASP.NET Core. O overload de mesmo nome em
    /// <c>Lumen.Authorization</c> (namespace distinto) registra apenas o núcleo — indicado para
    /// módulos ou libs que não hospedam middleware ASP.NET Core diretamente (ex.: módulos DDD).
    /// </remarks>
    /// <param name="services">A coleção de serviços.</param>
    /// <param name="configuration">Configuração da aplicação.</param>
    /// <param name="configure">Configuração opcional de <see cref="LumenAuthorizationOptions"/>.</param>
    public static IServiceCollection AddLumenAuthorization(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<LumenAuthorizationOptions>? configure = null)
    {
        CoreExtensions.AddLumenAuthorization(services, configuration, configure);
        services.AddLumenAuthorizationMigrations();
        services.AddLumenAuthorizationEnforcement();
        services.AddLumenAuthorizationDiscovery();

        return services;
    }

    public static IServiceCollection AddLumenAuthorizationEnforcement(this IServiceCollection services)
    {
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.TryAddSingleton<IUserIdAccessor, ClaimsUserIdAccessor>();

        return services;
    }

    public static IServiceCollection AddLumenAuthorizationDiscovery(this IServiceCollection services)
    {
        services.AddSingleton<PermissionDiscoveryScanner>();
        services.AddHostedService<PermissionDiscoveryAndReconciliationHostedService>();

        return services;
    }
}
