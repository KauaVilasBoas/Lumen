using Lumen.Authorization.Migrations.PostgreSQL.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Lumen.Authorization.Migrations.PostgreSQL;

/// <summary>
/// Extensões de DI para o assembly de migrations PostgreSQL da lib de autorização Lumen.
/// </summary>
public static class LumenAuthorizationPostgresMigrationsServiceCollectionExtensions
{
    /// <summary>
    /// Registra o hosted service que aplica as migrations do Lumen no banco PostgreSQL na
    /// inicialização da aplicação.
    /// </summary>
    /// <remarks>
    /// Equivalente a <c>AddLumenAuthorizationMigrations()</c> do pacote SQL Server.
    /// Deve ser chamado apenas quando o provider configurado for PostgreSQL.
    /// </remarks>
    public static IServiceCollection AddLumenAuthorizationPostgresMigrations(
        this IServiceCollection services)
    {
        services.AddHostedService<LumenAuthorizationPostgresMigrationsHostedService>();
        return services;
    }
}
