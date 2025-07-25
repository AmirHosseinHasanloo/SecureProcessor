using Microsoft.Extensions.Logging;
using SecureProcessor.Core.Patterns.Proxy;
using SecureProcessor.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
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
            _httpClient = httpClient;
            _serviceProxy = serviceProxy;
            _logger = logger;
        }

        /// <summary>
        /// Performs health check with retry and circuit breaker protection
        /// </summary>
        public async Task<HealthCheckResponse> CheckHealthAsync(string managerUrl)
        {
            return await _serviceProxy.InvokeAsync(async service =>
            {
                var request = new HealthCheckRequest
                {
                    Id = GenerateSystemGuid(),
                    SystemTime = DateTime.UtcNow,
                    NumberOfConnectedClients = 5
                };

                _logger.LogInformation($"Sending health check to {managerUrl}");

                var response = await _httpClient.PostAsJsonAsync($"{managerUrl}/api/module/health", request);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<HealthCheckResponse>();
                    _logger.LogInformation($"Health check successful: IsEnabled={result.IsEnabled}");
                    return result;
                }

                _logger.LogWarning($"Health check failed with status: {response.StatusCode}");
                throw new HttpRequestException($"Health check failed: {response.StatusCode}");
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