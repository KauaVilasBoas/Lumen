using Lumen.Identity.Infrastructure.Configuration;
using Lumen.Identity.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using CoreExtensions = Lumen.Identity.LumenIdentityServiceCollectionExtensions;
#pragma warning disable CA2000 // BuildServiceProvider is intentional here — options validated before first request

namespace Lumen.Identity.AspNetCore;

public static class LumenIdentityAspNetCoreServiceCollectionExtensions
{
    /// <summary>
    /// Registers Lumen.Identity for ASP.NET Core hosts: core services (DbContext, repositories,
    /// JWT, BCrypt, email), JWT Bearer authentication, and Authorization bridges.
    /// </summary>
    /// <remarks>
    /// Equivalent to calling <c>AddLumenIdentityCore(connectionString, configuration)</c>
    /// followed by <c>AddLumenIdentityAuthentication()</c>.
    ///
    /// Migrations are not applied by this method — add
    /// <c>Lumen.Identity.Migrations</c> (SQL Server) or
    /// <c>Lumen.Identity.Migrations.PostgreSQL</c> to your host project.
    /// </remarks>
    public static IServiceCollection AddLumenIdentity(
        this IServiceCollection services,
        string connectionString,
        IConfiguration configuration,
        Action<LumenIdentityOptions>? configure = null)
    {
        CoreExtensions.AddLumenIdentityCore(services, connectionString, configuration, configure);
        services.AddLumenIdentityAuthentication();
        return services;
    }

    /// <summary>
    /// Registers Lumen.Identity for ASP.NET Core hosts reading the connection string
    /// from <c>ConnectionStrings:DefaultConnection</c> and the provider from
    /// <c>Database:Provider</c>.
    /// </summary>
    public static IServiceCollection AddLumenIdentity(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<LumenIdentityOptions>? configure = null)
    {
        CoreExtensions.AddLumenIdentityCore(services, configuration, configure);
        services.AddLumenIdentityAuthentication();
        return services;
    }

    /// <summary>
    /// Registers JWT Bearer authentication and a default fallback policy that requires
    /// an authenticated user. Call this only when using the core extension directly
    /// instead of <see cref="AddLumenIdentity(IServiceCollection, IConfiguration, Action{LumenIdentityOptions}?)"/>.
    /// </summary>
    public static IServiceCollection AddLumenIdentityAuthentication(
        this IServiceCollection services,
        string signalRHubPath = "")
    {
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // Build service provider temporarily to resolve options.
                // This is safe here because AddJwtBearer delegates configuration
                // to the first request, not at registration time.
                var sp = services.BuildServiceProvider();
                var jwtOptions = sp.GetRequiredService<IOptions<IdentityJwtOptions>>().Value;
                options.TokenValidationParameters = IdentityJwtParametersBuilder.Build(jwtOptions);

                if (!string.IsNullOrEmpty(signalRHubPath))
                {
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var accessToken = context.Request.Query["access_token"];
                            var path = context.HttpContext.Request.Path;

                            if (!string.IsNullOrEmpty(accessToken) &&
                                path.StartsWithSegments(signalRHubPath))
                            {
                                context.Token = accessToken;
                            }

                            return Task.CompletedTask;
                        }
                    };
                }
            });

        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

        return services;
    }
}
