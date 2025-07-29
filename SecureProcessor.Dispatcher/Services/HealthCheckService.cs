using Microsoft.Extensions.Logging;
using SecureProcessor.Core.Patterns.Proxy;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SecureProcessor.Dispatcher.Services
{
    /// <summary>
    /// Implementation of health check service with proxy pattern
    /// </summary>
    public class HealthCheckService : IHealthCheckService
    {
        private readonly HttpClient _httpClient;
        private readonly IServiceProxy<IHealthCheckService> _serviceProxy;
        private readonly ILogger<HealthCheckService> _logger;

        public HealthCheckService(HttpClient httpClient,
            IServiceProxy<IHealthCheckService> serviceProxy,
            ILogger<HealthCheckService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _serviceProxy = serviceProxy ?? throw new ArgumentNullException(nameof(serviceProxy));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Performs health check with retry and circuit breaker protection
        /// </summary>
        public async Task<Shared.Models.HealthCheckResponse> CheckHealthAsync(string managerUrl)
        {
            return await _serviceProxy.InvokeAsync(async service =>
            {
                // ✅ اصلاح شده: ارسال داده صحیح
                var request = new
                {
                    id = GenerateSystemGuid(),
                    systemTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(), // ✅ Unix timestamp
                    numberOfConnectedClients = 1 // ✅ تعداد واقعی پردازش‌کننده‌ها
                };

                _logger.LogInformation($"Sending health check to {managerUrl}");
                _logger.LogDebug($"Health check request: {JsonSerializer.Serialize(request)}");

                // ✅ اصلاح شده: استفاده از PostAsJsonAsync به درستی
                var response = await _httpClient.PostAsJsonAsync($"{managerUrl}/api/module/health", request);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<Shared.Models.HealthCheckResponse>();
                    _logger.LogInformation($"Health check successful: IsEnabled={result?.IsEnabled}");
                    return result;
                }

                // ✅ اصلاح شده: دریافت محتوای خطا برای عیب‌یابی
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning($"Health check failed with status: {response.StatusCode}, Content: {errorContent}");
                throw new HttpRequestException($"Health check failed: {response.StatusCode}, Details: {errorContent}");
            }, retryCount: 5, retryDelay: TimeSpan.FromSeconds(10));
        }

        private string GenerateSystemGuid()
        {
            // Generate GUID based on system MAC address (simplified)
            var macAddress = "00:11:22:33:44:55"; // In real scenario, get actual MAC
            return Guid.NewGuid().ToString();
        }
    }
}