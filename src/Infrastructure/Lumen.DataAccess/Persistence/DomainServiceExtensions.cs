using Lumen.Domain.Notifications;
using Lumen.Domain.Users;
using Microsoft.Extensions.DependencyInjection;

namespace Lumen.DataAccess.Persistence;

public static class DomainServiceExtensions
{
    public static IServiceCollection AddDomainServices(this IServiceCollection services)
    {
        services.AddScoped<IEmailConfirmationService, EmailConfirmationService>();
        services.AddScoped<IUserPasswordService, UserPasswordService>();

        return services;
    }
}
