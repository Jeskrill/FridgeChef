using FridgeChef.Auth.Application.UseCases;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace FridgeChef.Auth.Application.DependencyInjection;

public static class AuthApplicationExtensions
{
    public static IServiceCollection AddAuthApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<LoginValidator>(ServiceLifetime.Scoped);

        services.AddScoped<LoginHandler>();
        services.AddScoped<RegisterHandler>();
        services.AddScoped<LogoutHandler>();
        services.AddScoped<RefreshTokenHandler>();
        services.AddScoped<GetProfileHandler>();
        services.AddScoped<UpdateProfileHandler>();
        services.AddScoped<ChangePasswordHandler>();

        return services;
    }
}
