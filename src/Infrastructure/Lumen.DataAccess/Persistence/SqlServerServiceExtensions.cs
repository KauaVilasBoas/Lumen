using Lumen.DataAccess.Persistence.Repositories;
using Lumen.Domain.Audit;
using Lumen.Domain.Authorization;
using Lumen.Domain.Tokens;
using Lumen.Domain.Users;
using Lumen.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lumen.DataAccess.Persistence;

public static class SqlServerServiceExtensions
{
    public static IServiceCollection AddRelationalDataAccess(this IServiceCollection services)
    {
        services.AddDbContext<LumenDbContext>((serviceProvider, options) =>
        {
            var sqlServerOptions = serviceProvider
                .GetRequiredService<IOptions<SqlServerOptions>>()
                .Value;

            options.UseSqlServer(
                sqlServerOptions.ConnectionString,
                sql => sql.MigrationsAssembly("Lumen.Migrations"));
        });

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
        services.AddScoped<IEmailConfirmationTokenRepository, EmailConfirmationTokenRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IGroupPermissionRepository, GroupPermissionRepository>();
        services.AddScoped<IProfileRepository, ProfileRepository>();
        services.AddScoped<IUserProfileRepository, UserProfileRepository>();
        services.AddScoped<IAuditRepository, AuditRepository>();

        return services;
    }
}
