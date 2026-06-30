using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Lumen.Modules.Audit.Persistence;
using Lumen.Modules.Identity.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.MsSql;
using Testcontainers.Redis;

namespace Lumen.IntegrationTests.Infrastructure;

/// <summary>
/// Single shared fixture for the entire integration test suite.
/// Owns one SQL Server container and one Redis container; both are started
/// once per test collection and torn down after the last test completes.
///
/// HTTP tests call <see cref="CreateAnonymousClient"/> or <see cref="CreateAuthenticatedClient"/>.
/// DbContext access uses <see cref="CreateIdentityDbContext"/> or <see cref="CreateAuditDbContext"/>.
///
/// Design note: xUnit's ICollectionFixture guarantees exactly one instance per
/// [Collection] across all test classes that belong to it.
/// </summary>
public sealed class IntegrationFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    internal const string TestJwtIssuer = "lumen-test";
    internal const string TestJwtAudience = "lumen-test";
    internal const string TestJwtSecret = "lumen-test-secret-key-min-32-chars!!";

    private readonly MsSqlContainer _sqlContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    private readonly RedisContainer _redisContainer = new RedisBuilder()
        .WithImage("redis:7")
        .Build();

    async Task IAsyncLifetime.InitializeAsync()
    {
        await Task.WhenAll(
            _sqlContainer.StartAsync(),
            _redisContainer.StartAsync());

        await ApplyModuleMigrationsAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _sqlContainer.DisposeAsync();
        await _redisContainer.DisposeAsync();
        await base.DisposeAsync();
    }

    /// <summary>Creates an <see cref="IdentityDbContext"/> connected to the shared SQL Server container.</summary>
    internal IdentityDbContext CreateIdentityDbContext()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseSqlServer(_sqlContainer.GetConnectionString())
            .Options;

        return new IdentityDbContext(options);
    }

    /// <summary>Creates an <see cref="AuditDbContext"/> connected to the shared SQL Server container.</summary>
    internal AuditDbContext CreateAuditDbContext()
    {
        var options = new DbContextOptionsBuilder<AuditDbContext>()
            .UseSqlServer(_sqlContainer.GetConnectionString())
            .Options;

        return new AuditDbContext(options);
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

    public HttpClient CreateClientWithBrokenRedis(string userId)
    {
        var token = BuildValidJwt(userId);
        var client = WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IDistributedCache>();
                services.AddSingleton<IDistributedCache, BrokenDistributedCache>();
            });
        }).CreateClient();

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _sqlContainer.GetConnectionString(),
                ["ConnectionStrings:Redis"] = _redisContainer.GetConnectionString(),
                ["Jwt:Issuer"] = TestJwtIssuer,
                ["Jwt:Audience"] = TestJwtAudience,
                ["Jwt:Secret"] = TestJwtSecret,
                ["Jwt:ExpirationMinutes"] = "15",
                ["Jwt:RefreshExpirationDays"] = "7",
                ["Redis:InstanceName"] = "lumen-test:",
                ["Smtp:Host"] = "localhost",
                ["Smtp:Port"] = "1025",
                ["Smtp:From"] = "test@lumen.local",
                ["Hibp:UserAgent"] = "Lumen-Tests/1.0",
                ["Hibp:ApiBaseUrl"] = "https://api.pwnedpasswords.com/",
                ["App:BaseUrl"] = "http://localhost:5000",
                ["App:LockoutThreshold"] = "5",
                ["App:LockoutDurationMinutes"] = "15",
                ["App:RefreshTokenExpirationDays"] = "7",
                ["SqlServer:ConnectionString"] = _sqlContainer.GetConnectionString(),
                ["Hangfire:Dashboard:Path"] = "/internal/jobs-admin",
                ["Hangfire:Dashboard:Username"] = "admin",
                ["Hangfire:Dashboard:Password"] = "admin",
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IHostedService>();
        });
    }

    private async Task ApplyModuleMigrationsAsync()
    {
        await using var identityDb = CreateIdentityDbContext();
        await identityDb.Database.MigrateAsync();

        await using var auditDb = CreateAuditDbContext();
        await auditDb.Database.MigrateAsync();
    }

    public string BuildJwtForUser(string userId) => BuildValidJwt(userId);

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

[CollectionDefinition(Name)]
public sealed class IntegrationCollection : ICollectionFixture<IntegrationFixture>
{
    public const string Name = "Integration";
}
