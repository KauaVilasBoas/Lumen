using Lumen.Modules.Identity.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lumen.Modules.Identity.Infrastructure.Security;

public static class IdentityAuthServiceCollectionExtensions
{
    public static IServiceCollection AddIdentityJwtBearerAuthentication(
        this IServiceCollection services,
        string signalRHubPath = "")
    {
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                var sp = services.BuildServiceProvider();
                var jwtOptions = sp.GetRequiredService<IOptions<IdentityJwtOptions>>().Value;
                options.TokenValidationParameters = JwtService.BuildValidationParameters(jwtOptions);

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
