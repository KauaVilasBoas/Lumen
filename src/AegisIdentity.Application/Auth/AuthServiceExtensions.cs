using AegisIdentity.Application.Auth.Register;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace AegisIdentity.Application.Auth;

public static class AuthServiceExtensions
{
    public static IServiceCollection AddAuthUseCases(this IServiceCollection services)
    {
        services.AddScoped<IValidator<RegisterRequest>, RegisterRequestValidator>();
        services.AddScoped<IRegisterUserUseCase, RegisterUserUseCase>();
        return services;
    }
}
