using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AegisIdentity.DataAccess.Persistence;
using Hangfire;
using Hangfire.InMemory;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace AegisIdentity.IntegrationTests.Authorization;

public sealed class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
    internal const string TestJwtIssuer = "aegis-test";
    internal const string TestJwtAudience = "aegis-test";
    internal const string TestJwtSecret = "aegis-test-secret-key-min-32-chars!!";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = TestJwtIssuer,
                ["Jwt:Audience"] = TestJwtAudience,
                ["Jwt:Secret"] = TestJwtSecret,
                ["Jwt:ExpirationMinutes"] = "15",
                ["Jwt:RefreshExpirationDays"] = "7",
                ["SqlServer:ConnectionString"] = "Server=.;Database=aegis_test;Trusted_Connection=True;",
                ["Smtp:Host"] = "localhost",
                ["Smtp:Port"] = "1025",
                ["Smtp:From"] = "test@aegis.local",
                ["Hibp:UserAgent"] = "AegisIdentity-Tests/1.0",
                ["Hibp:ApiBaseUrl"] = "https://api.pwnedpasswords.com/",
                ["App:BaseUrl"] = "http://localhost:5000",
                ["App:LockoutThreshold"] = "5",
                ["App:LockoutDurationMinutes"] = "15",
                ["App:RefreshTokenExpirationDays"] = "7",
                ["Hangfire:Dashboard:Path"] = "/internal/jobs-admin",
                ["Hangfire:Dashboard:Username"] = "admin",
                ["Hangfire:Dashboard:Password"] = "admin",
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IHostedService>();

            services.RemoveAll<DbContextOptions<AegisIdentityDbContext>>();
            services.RemoveAll<AegisIdentityDbContext>();

            services.AddDbContext<AegisIdentityDbContext>(options =>
                options.UseInMemoryDatabase("aegis_test_authz"));

            services.AddHangfire(config =>
                config.UseInMemoryStorage());
        });
    }

    public HttpClient CreateAnonymousClient() => CreateClient();

    public HttpClient CreateAuthenticatedClient(string userId = "00000000-0000-0000-0000-000000000001")
    {
        var token = BuildValidJwt(userId);
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static string BuildValidJwt(string userId)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: TestJwtIssuer,
            audience: TestJwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
