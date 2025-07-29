// در ServiceProxy.cs
using Microsoft.Extensions.Logging;
using SecureProcessor.Core.Patterns.CircuitBreaker;
using SecureProcessor.Core.Patterns.Proxy;

public class ServiceProxy<T> : IServiceProxy<T> where T : class
{
    // ✅ حذف وابستگی مستقیم به T
    private readonly Func<T> _serviceFactory;
    private readonly ICircuitBreaker _circuitBreaker;
    private readonly ILogger<ServiceProxy<T>> _logger;

    // ✅ استفاده از Factory Method به جای وابستگی مستقیم
    public ServiceProxy(Func<T> serviceFactory, ICircuitBreaker circuitBreaker, ILogger<ServiceProxy<T>> logger)
    {
        _serviceFactory = serviceFactory ?? throw new ArgumentNullException(nameof(serviceFactory));
        _circuitBreaker = circuitBreaker ?? throw new ArgumentNullException(nameof(circuitBreaker));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<TResult> InvokeAsync<TResult>(Func<T, Task<TResult>> operation,
        int retryCount = 3,
        TimeSpan retryDelay = default)
    {
        if (retryDelay == default)
            retryDelay = TimeSpan.FromSeconds(1);

        return await _circuitBreaker.ExecuteAsync(async () =>
        {
            for (int attempt = 0; attempt <= retryCount; attempt++)
            {
                try
                {
                    // ✅ ایجاد سرویس در هر تلاش
                    var service = _serviceFactory();
                    return await operation(service);
                }
                catch (Exception ex) when (attempt < retryCount)
                {
                    _logger.LogWarning($"Attempt {attempt + 1} failed: {ex.Message}. Retrying in {retryDelay.TotalSeconds}s...");
                    await Task.Delay(retryDelay);
                }
            }
            throw new InvalidOperationException("All retry attempts failed");
        });
    }

    public async Task InvokeAsync(Func<T, Task> operation,
        int retryCount = 3,
        TimeSpan retryDelay = default)
    {
        if (retryDelay == default)
            retryDelay = TimeSpan.FromSeconds(1);

        await _circuitBreaker.ExecuteAsync(async () =>
        {
            for (int attempt = 0; attempt <= retryCount; attempt++)
            {
                try
                {
                    // ✅ ایجاد سرویس در هر تلاش
                    var service = _serviceFactory();
                    await operation(service);
                    return;
                }
                catch (Exception ex) when (attempt < retryCount)
                {
                    _logger.LogWarning($"Attempt {attempt + 1} failed: {ex.Message}. Retrying in {retryDelay.TotalSeconds}s...");
                    await Task.Delay(retryDelay);
                }
            }
            throw new InvalidOperationException("All retry attempts failed");
        });
    }
}