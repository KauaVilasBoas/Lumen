using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AegisIdentity.DataAccess.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.MsSql;
using Testcontainers.Redis;

namespace AegisIdentity.IntegrationTests.Infrastructure;

/// <summary>
/// Single shared fixture for the entire integration test suite.
/// Owns one SQL Server container and one Redis container; both are started
/// once per test collection and torn down after the last test completes.
///
/// Repository tests call <see cref="CreateDbContext"/> directly.
/// HTTP tests call <see cref="CreateAnonymousClient"/> or <see cref="CreateAuthenticatedClient"/>.
///
/// Design note: xUnit's ICollectionFixture guarantees exactly one instance per
/// [Collection] across all test classes that belong to it.  Keeping a single
/// fixture avoids the two-container problem where AuthorizationWebApplicationFactory
/// previously spun up its own MsSqlContainer independently of SqlServerFixture.
/// </summary>
public sealed class IntegrationFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    internal const string TestJwtIssuer = "aegis-test";
    internal const string TestJwtAudience = "aegis-test";
    internal const string TestJwtSecret = "aegis-test-secret-key-min-32-chars!!";

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

        await ApplyMigrationsAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _sqlContainer.DisposeAsync();
        await _redisContainer.DisposeAsync();
        await base.DisposeAsync();
    }

    /// <summary>Creates a <see cref="AegisIdentityDbContext"/> connected to the shared SQL Server container.</summary>
    public AegisIdentityDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AegisIdentityDbContext>()
            .UseSqlServer(
                _sqlContainer.GetConnectionString(),
                sql => sql.MigrationsAssembly("AegisIdentity.Migrations"))
            .Options;

        return new AegisIdentityDbContext(options);
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
                ["SqlServer:ConnectionString"] = _sqlContainer.GetConnectionString(),
                ["Redis:ConnectionString"] = _redisContainer.GetConnectionString(),
                ["Redis:InstanceName"] = "aegis-test:",
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
        });
    }

    private async Task ApplyMigrationsAsync()
    {
        await using var dbContext = CreateDbContext();
        await dbContext.Database.MigrateAsync();
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

[CollectionDefinition(Name)]
public sealed class IntegrationCollection : ICollectionFixture<IntegrationFixture>
{
    public const string Name = "Integration";
}
