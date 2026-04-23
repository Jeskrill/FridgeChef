using FridgeChef.Catalog.Application.UseCases.MatchFromPantry;
using FridgeChef.Pantry.Application.UseCases;
using FridgeChef.Pantry.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FridgeChef.Pantry.Infrastructure;

public static class PantryInfrastructureExtensions
{
    public static IServiceCollection AddPantryInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<PantryDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IPantryRepository, PantryRepository>();

        services.AddScoped<IPantrySupplier, PantrySupplierAdapter>();

        return services;
    }
}
