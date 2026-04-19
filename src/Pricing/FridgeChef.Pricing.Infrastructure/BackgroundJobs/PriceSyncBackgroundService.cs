using FridgeChef.Pricing.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FridgeChef.Pricing.Infrastructure.BackgroundJobs;

/// <summary>
/// Runs the daily price sync. 
/// Auto-sync is DISABLED by default — use POST /admin/pricing/sync to trigger manually.
/// Set Pricing:AutoSync=true in config to enable automatic daily sync.
/// </summary>
public sealed class PriceSyncBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PriceSyncBackgroundService> _logger;
    private readonly IConfiguration _config;

    private static readonly TimeSpan SyncInterval = TimeSpan.FromHours(24);

    public PriceSyncBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<PriceSyncBackgroundService> logger,
        IConfiguration config)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var autoSync = _config.GetValue<bool>("Pricing:AutoSync", false);

        if (!autoSync)
        {
            _logger.LogInformation(
                "Price sync auto-start is DISABLED. Use POST /admin/pricing/sync to trigger manually. " +
                "Set Pricing:AutoSync=true to enable.");
            return; // Exit — no background loop
        }

        _logger.LogInformation("Price sync background service started. First sync in 5 minutes.");
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var syncService = scope.ServiceProvider.GetRequiredService<PriceSyncService>();

                _logger.LogInformation("Starting scheduled price sync...");
                await syncService.SyncAllAsync(stoppingToken);
                _logger.LogInformation("Scheduled price sync completed. Next sync in {Interval}", SyncInterval);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Price sync failed. Will retry in {Interval}", SyncInterval);
            }

            await Task.Delay(SyncInterval, stoppingToken);
        }
    }
}
