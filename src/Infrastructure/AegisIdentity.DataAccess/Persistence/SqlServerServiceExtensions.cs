using AegisIdentity.DataAccess.Persistence.Repositories;
using AegisIdentity.Domain.Tokens;
using AegisIdentity.Domain.Users;
using AegisIdentity.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AegisIdentity.DataAccess.Persistence;

public static class SqlServerServiceExtensions
{
    public static IServiceCollection AddRelationalDataAccess(this IServiceCollection services)
    {
        services.AddDbContext<AegisIdentityDbContext>((serviceProvider, options) =>
        {
            var sqlServerOptions = serviceProvider
                .GetRequiredService<IOptions<SqlServerOptions>>()
                .Value;

            options.UseSqlServer(sqlServerOptions.ConnectionString);
        });

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
        services.AddScoped<IEmailConfirmationTokenRepository, EmailConfirmationTokenRepository>();

        return services;
    }
}
