using Microsoft.Extensions.Options;
using ServicosApp.Application.DTOs.Fiscal;
using ServicosApp.Application.Interfaces;

namespace ServicosApp.API;

public sealed class FiscalPendingSyncWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptionsMonitor<FiscalPendingSyncOptions> _optionsMonitor;
    private readonly ILogger<FiscalPendingSyncWorker> _logger;

    public FiscalPendingSyncWorker(
        IServiceScopeFactory scopeFactory,
        IOptionsMonitor<FiscalPendingSyncOptions> optionsMonitor,
        ILogger<FiscalPendingSyncWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _optionsMonitor = optionsMonitor;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var startupDelay = TimeSpan.FromSeconds(Math.Max(0, _optionsMonitor.CurrentValue.StartupDelaySeconds));
        if (startupDelay > TimeSpan.Zero)
            await Task.Delay(startupDelay, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var options = _optionsMonitor.CurrentValue;

            if (options.Enabled)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var syncService = scope.ServiceProvider.GetRequiredService<IFiscalPendingSyncService>();
                    var result = await syncService.SynchronizePendingAsync(stoppingToken);

                    if (result.ScannedCount > 0 || result.FailedCount > 0)
                    {
                        _logger.LogInformation(
                            "Sincronizacao fiscal pendente: consultados={ProcessedCount}, autorizados={AuthorizedCount}, cancelados={CancelledCount}, pendentes={StillPendingCount}, falhas={FailedCount}.",
                            result.ProcessedCount,
                            result.AuthorizedCount,
                            result.CancelledCount,
                            result.StillPendingCount,
                            result.FailedCount);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Falha no worker de sincronizacao fiscal pendente.");
                }
            }

            var interval = TimeSpan.FromSeconds(Math.Max(30, options.IntervalSeconds));
            await Task.Delay(interval, stoppingToken);
        }
    }
}
