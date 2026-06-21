using Lumen.Domain.Security;
using Lumen.Infrastructure.Configuration;
using Lumen.SharedKernel.Constants;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lumen.Infrastructure.Security;

public static class SecurityServiceExtensions
{
    public static IServiceCollection AddSecurity(this IServiceCollection services)
    {
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<IJwtService, JwtService>();
        services.AddScoped<IPasswordValidator, PasswordValidator>();

        // Resolve JwtOptions here so the JwtBearer middleware uses exactly the same
        // validation parameters that JwtService.ValidateToken uses — single source of truth.
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // Build a temporary service provider scoped to this configuration lambda
                // so we can resolve IOptions<JwtOptions> before the full DI root is built.
                // Using BuildServiceProvider() during configuration is acceptable here
                // because this is a one-time setup call, not a per-request resolution.
                var sp = services.BuildServiceProvider();
                var jwtOptions = sp.GetRequiredService<IOptions<JwtOptions>>().Value;
                options.TokenValidationParameters = JwtService.BuildValidationParameters(jwtOptions);

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;

                        if (!string.IsNullOrEmpty(accessToken) &&
                            path.StartsWithSegments(HubRoutes.AuthorizationGraph))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    }
                };
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
