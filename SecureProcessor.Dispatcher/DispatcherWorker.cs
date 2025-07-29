using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SecureProcessor.Core.Patterns.Options;
using SecureProcessor.Dispatcher.Services;

namespace SecureProcessor.Dispatcher;

public class DispatcherWorker : BackgroundService
{
    private readonly ILogger<DispatcherWorker> _logger;
    private readonly IHealthCheckService _healthCheckService;
    private readonly IProcessorManagerService _processorManager;
    private readonly DispatcherOptions _options;

    public DispatcherWorker(
        ILogger<DispatcherWorker> logger,
        IHealthCheckService healthCheckService,
        IProcessorManagerService processorManager,
        IOptions<DispatcherOptions> options)
    {
        _logger = logger;
        _healthCheckService = healthCheckService;
        _processorManager = processorManager;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 Dispatcher worker started");
        _logger.LogInformation("🔧 Health check interval: {interval} seconds", _options.HealthCheckInterval);

        try
        {
            // اجرای health check اولیه
            await PerformHealthCheck(stoppingToken);

            // حلقه اصلی سرویس
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // ✅ سلامت‌سنجی دوره‌ای
                    await PerformHealthCheck(stoppingToken);

                    // ✅ پاک‌سازی پردازش‌کننده‌های غیرفعال
                    await _processorManager.CleanupInactiveProcessorsAsync();

                    // ✅ لاگ وضعیت فعلی
                    LogSystemStatus();

                    // انتظار برای دوره بعدی
                    await Task.Delay(TimeSpan.FromSeconds(_options.HealthCheckInterval), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // نادیده گرفتن خطا در زمان توقف سرویس
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error in dispatcher worker loop");
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); // تاخیر در صورت خطا
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Fatal error in dispatcher worker");
        }
        finally
        {
            _logger.LogInformation("🛑 Dispatcher worker stopped");
        }
    }

    private async Task PerformHealthCheck(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("🏥 Performing health check...");
            var healthResponse = await _healthCheckService.CheckHealthAsync(_options.ManagerServiceUrl);

            if (healthResponse != null)
            {
                _logger.LogInformation("✅ Health check completed - IsEnabled: {isEnabled}, ActiveClients: {active}",
                    healthResponse.IsEnabled, healthResponse.NumberOfActiveClients);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Health check failed");
        }
    }

    private void LogSystemStatus()
    {
        var activeProcessors = 0;
        try
        {
            activeProcessors = _processorManager.GetActiveProcessorsCount();
        }
        catch
        {
            // نادیده گرفتن خطا در لاگ‌نویسی
        }

        _logger.LogInformation("📊 System status - Active processors: {count}", activeProcessors);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("🟡 Dispatcher worker is stopping...");
        await base.StopAsync(cancellationToken);
    }
}