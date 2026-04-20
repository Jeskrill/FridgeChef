using FridgeChef.Admin.Application.UseCases;
using Microsoft.Extensions.DependencyInjection;

namespace FridgeChef.Admin.Application.DependencyInjection;

public static class AdminApplicationExtensions
{
    public static IServiceCollection AddAdminApplication(this IServiceCollection services)
    {
        services.AddScoped<GetAdminUsersHandler>();
        services.AddScoped<SetUserBlockedHandler>();
        services.AddScoped<GetAdminStatsHandler>();
        return services;
    }
}
