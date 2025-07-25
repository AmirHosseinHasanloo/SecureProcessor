using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SecureProcessor.Core.Patterns.Options;
using SecureProcessor.Core.Services;
using SecureProcessor.Dispatcher.gRPC_Services;
using SecureProcessor.Dispatcher.Services;

namespace SecureProcessor.Dispatcher;

public class DispatcherWorker : BackgroundService
{
    private readonly ILogger<DispatcherWorker> _logger;
    private readonly IHealthCheckService _healthCheckService;
    private readonly IMessageQueueService _messageQueueService;
    private readonly IProcessorManagerService _processorManager;
    private readonly DispatcherOptions _options;
    private readonly MessageDispatcherService _grpcService;

    public DispatcherWorker(
        ILogger<DispatcherWorker> logger,
        IHealthCheckService healthCheckService,
        IMessageQueueService messageQueueService,
        IProcessorManagerService processorManager,
        IOptions<DispatcherOptions> options)
    {
        _logger = logger;
        _healthCheckService = healthCheckService;
        _messageQueueService = messageQueueService;
        _processorManager = processorManager;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Dispatcher worker started");

        try
        {
            // اجرای health check اولیه
            var healthResponse = await _healthCheckService.CheckHealthAsync(_options.ManagerServiceUrl);
            _logger.LogInformation("Initial health check: IsEnabled={isEnabled}", healthResponse?.IsEnabled);

            // اینجا می‌توانید سرور gRPC را راه‌اندازی کنید
            // یا منطق پردازش پیام را اجرا کنید

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                _logger.LogInformation("Dispatcher is running...");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in dispatcher worker");
        }
    }
}