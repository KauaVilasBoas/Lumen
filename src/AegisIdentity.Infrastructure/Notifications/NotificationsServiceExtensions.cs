using AegisIdentity.Application.Notifications;
using AegisIdentity.Domain.Notifications;
using Microsoft.Extensions.DependencyInjection;

namespace AegisIdentity.Infrastructure.Notifications;

public static class NotificationsServiceExtensions
{
    public static IServiceCollection AddNotifications(this IServiceCollection services)
    {
        // Renderer is stateless and caches templates in a static dictionary — singleton.
        services.AddSingleton<EmailTemplateRenderer>();

        // Adapter exposes the concrete renderer through the Application abstraction.
        services.AddSingleton<IEmailTemplateRenderer, EmailTemplateRendererAdapter>();

        // Transport opens a fresh SmtpClient per call (MailKit guidance for short-lived sends).
        // Scoped is sufficient and aligns with the rest of the Infra registrations.
        services.AddScoped<ISmtpTransport, MailKitSmtpTransport>();
        services.AddScoped<IEmailService, MailKitEmailService>();

        return services;
    }
}
