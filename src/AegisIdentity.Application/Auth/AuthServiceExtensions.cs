using AegisIdentity.Application.Auth.Login;
using AegisIdentity.Application.Auth.Register;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace AegisIdentity.Application.Auth;

public static class AuthServiceExtensions
{
    /// <summary>
    /// Registers FluentValidation validators for the auth request DTOs.
    /// Use-case implementations are no longer registered here — requests are
    /// dispatched through MediatR (<c>services.AddMediatR</c> in the Api root).
    /// </summary>
    public static IServiceCollection AddAuthValidators(this IServiceCollection services)
    {
        services.AddScoped<IValidator<RegisterRequest>, RegisterRequestValidator>();
        services.AddScoped<IValidator<LoginRequest>, LoginRequestValidator>();

        return services;
    }
}
