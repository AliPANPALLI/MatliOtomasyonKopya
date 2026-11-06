using System.Diagnostics;
using ITG.Edge.Application;
using Microsoft.Extensions.Options;

namespace ITG.Edge.Worker;

public sealed class ScanBackgroundService(MeasurementScanService service, IOptions<ItgOptions> settings,
    ILogger<ScanBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var watch = Stopwatch.StartNew();
            try
            {
                await service.ExecuteScanAsync(ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Scan failed; will retry on next cycle");
            }
            var delay = TimeSpan.FromSeconds(Math.Max(1, settings.Value.ScanIntervalSeconds)) - watch.Elapsed;
            try
            {
                await Task.Delay(delay > TimeSpan.FromSeconds(1) ? delay : TimeSpan.FromSeconds(1), ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
