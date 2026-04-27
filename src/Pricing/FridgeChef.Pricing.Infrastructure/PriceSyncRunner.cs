using FridgeChef.Pricing.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FridgeChef.Pricing.Infrastructure;

public sealed class PriceSyncRunner
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PriceSyncRunner> _logger;

    private int _isRunning;

    public PriceSyncRunner(
        IServiceScopeFactory scopeFactory,
        ILogger<PriceSyncRunner> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public bool IsRunning => Volatile.Read(ref _isRunning) == 1;

    public async Task<bool> TryRunAsync(CancellationToken ct)
    {
        if (Interlocked.CompareExchange(ref _isRunning, 1, 0) != 0)
            return false;

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var syncService = scope.ServiceProvider.GetRequiredService<PriceSyncService>();
            await syncService.SyncAllAsync(ct);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Price sync runner failed.");
            throw;
        }
        finally
        {
            Volatile.Write(ref _isRunning, 0);
        }
    }
}
