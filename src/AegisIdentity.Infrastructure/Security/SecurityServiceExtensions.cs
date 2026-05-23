using System.Net.Http.Headers;
using AegisIdentity.Application.Security;
using AegisIdentity.Domain.Security;
using AegisIdentity.Infrastructure.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AegisIdentity.Infrastructure.Security;

public static class SecurityServiceExtensions
{
    internal static readonly TimeSpan PwnedPasswordsTimeout = TimeSpan.FromSeconds(2);

    public static IServiceCollection AddSecurity(this IServiceCollection services)
    {
        services.AddMemoryCache();

        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<IJwtService, JwtService>();


        services
            .AddHttpClient<IPwnedPasswordsClient, PwnedPasswordsClient>((sp, client) =>
            {
                var options = sp.GetRequiredService<IOptions<HibpOptions>>().Value;

                client.BaseAddress = new Uri(EnsureTrailingSlash(options.ApiBaseUrl));
                client.Timeout = PwnedPasswordsTimeout;
                client.DefaultRequestHeaders.UserAgent.Clear();
                client.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);
                // Add-Padding masks the real range size on the wire to defend against
                // traffic-analysis correlation. See: https://haveibeenpwned.com/API/v3#PwnedPasswords
                client.DefaultRequestHeaders.Add("Add-Padding", "true");
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
            });

        return services;
    }

    private static string EnsureTrailingSlash(string baseUrl)
        => baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/";
}
