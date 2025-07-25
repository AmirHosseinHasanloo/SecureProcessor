using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SecureProcessor.Shared.Models;

namespace SecureProcessor.Manager.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ModuleController : ControllerBase
    {
        private readonly ILogger<ModuleController> _logger;
        private static int _connectedClients = 0;
        private static int _totalHealthChecks = 0;
        private static DateTime _lastHealthCheck = DateTime.MinValue;
        private static bool _systemOverloaded = false;
        private static readonly object _lock = new object();

        public ModuleController(ILogger<ModuleController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Health check endpoint for system monitoring
        /// </summary>
        [HttpPost("health")]
        public IActionResult HealthCheck([FromBody] HealthCheckRequest request)
        {
            // لاگ دریافت درخواست
            _logger.LogInformation("    HEALTH CHECK REQUEST RECEIVED");
            _logger.LogInformation($"   Client ID: {request?.Id ?? "NULL"}");
            _logger.LogInformation($"   System Time: {request?.SystemTime:yyyy-MM-dd HH:mm:ss} UTC");
            _logger.LogInformation($"   Connected Clients: {request?.NumberOfConnectedClients ?? 0}");

            // Validate request
            if (request == null)
            {
                _logger.LogError("HEALTH CHECK FAILED: Request is NULL");
                return BadRequest("Request body is required");
            }

            if (string.IsNullOrEmpty(request.Id))
            {
                _logger.LogWarning("   HEALTH CHECK WARNING: Missing Client ID");
                return BadRequest("Id is required");
            }

            // افزایش شمارنده درخواست‌ها
            int requestNumber;
            lock (_lock)
            {
                _totalHealthChecks++;
                requestNumber = _totalHealthChecks;
                _lastHealthCheck = DateTime.UtcNow;
            }

            _logger.LogInformation($"   Request #{requestNumber} from client {request.Id}");

            // Update system metrics
            UpdateSystemMetrics(request, requestNumber);

            // Check system health
            var systemHealth = CheckSystemHealth(requestNumber);

            var response = new HealthCheckResponse
            {
                IsEnabled = systemHealth.IsEnabled,
                NumberOfActiveClients = systemHealth.ActiveClients,
                ExpirationTime = systemHealth.ExpirationTime
            };

            // لاگ پاسخ
            _logger.LogInformation($"   HEALTH CHECK RESPONSE #{requestNumber}");
            _logger.LogInformation($"   IsEnabled: {response.IsEnabled}");
            _logger.LogInformation($"   ActiveClients: {response.NumberOfActiveClients}");
            _logger.LogInformation($"   ExpirationTime: {response.ExpirationTime:yyyy-MM-dd HH:mm:ss} UTC");

            if (!response.IsEnabled)
            {
                _logger.LogWarning($"   SYSTEM DISABLED - Response #{requestNumber}");
            }

            return Ok(response);
        }

        private void UpdateSystemMetrics(HealthCheckRequest request, int requestNumber)
        {
            lock (_lock)
            {
                int previousClients = _connectedClients;
                _connectedClients = request.NumberOfConnectedClients;

                _logger.LogInformation($"   METRICS UPDATE #{requestNumber}");
                _logger.LogInformation($"   Previous Connected Clients: {previousClients}");
                _logger.LogInformation($"   Current Connected Clients: {_connectedClients}");
                _logger.LogInformation($"   Change: {(_connectedClients - previousClients):+0;-0;0} clients");
            }
        }

        private SystemHealthStatus CheckSystemHealth(int requestNumber)
        {
            lock (_lock)
            {
                _logger.LogInformation($"   SYSTEM HEALTH CHECK #{requestNumber} STARTED");
                _logger.LogInformation($"   Current Connected Clients: {_connectedClients}");

                // منطق واقعی بررسی سلامت سیستم
                bool isEnabled = true;
                int activeClients = Math.Min(_connectedClients, 10); // حداکثر 10 کلاینت
                DateTime expirationTime = DateTime.UtcNow.AddMinutes(10);

                _logger.LogInformation($"   Configured Max Active Clients: 10");
                _logger.LogInformation($"   Calculated Active Clients: {activeClients}");

                // بررسی شرایط بار بالا
                bool previousOverload = _systemOverloaded;
                _systemOverloaded = _connectedClients > 15; // آستانه بار بالا

                if (_systemOverloaded)
                {
                    _logger.LogWarning($"   HIGH LOAD DETECTED #{requestNumber}");
                    _logger.LogWarning($"   Connected Clients ({_connectedClients}) > Threshold (15)");

                    if (_connectedClients > 20)
                    {
                        isEnabled = false;
                        _logger.LogError($"   SYSTEM OVERLOADED #{requestNumber}");
                        _logger.LogError($"   CRITICAL: {_connectedClients} clients > MAX (20)");
                        _logger.LogError($"   SYSTEM DISABLED FOR SAFETY");
                    }
                }
                else if (previousOverload)
                {
                    _logger.LogInformation($"  LOAD NORMALIZED #{requestNumber}");
                    _logger.LogInformation($"   System recovered from high load state");
                }

                // بررسی سلامت کلی
                if (isEnabled)
                {
                    _logger.LogInformation($"  SYSTEM HEALTH OK #{requestNumber}");
                }
                else
                {
                    _logger.LogError($"  SYSTEM HEALTH CRITICAL #{requestNumber}");
                }

                var healthStatus = new SystemHealthStatus
                {
                    IsEnabled = isEnabled,
                    ActiveClients = activeClients,
                    ExpirationTime = expirationTime
                };

                _logger.LogInformation($"   HEALTH CHECK #{requestNumber} COMPLETED");
                _logger.LogInformation($"   Final Status - Enabled: {healthStatus.IsEnabled}, Active: {healthStatus.ActiveClients}");

                return healthStatus;
            }
        }

        /// <summary>
        /// Endpoint برای مانیتورینگ وضعیت سیستم
        /// </summary>
        [HttpGet("status")]
        public IActionResult GetSystemStatus()
        {
            lock (_lock)
            {
                var status = new
                {
                    IsEnabled = !_systemOverloaded || _connectedClients <= 20,
                    ConnectedClients = _connectedClients,
                    TotalHealthChecks = _totalHealthChecks,
                    LastHealthCheck = _lastHealthCheck,
                    SystemOverloaded = _systemOverloaded,
                    CurrentTime = DateTime.UtcNow,
                    Uptime = DateTime.UtcNow - System.Diagnostics.Process.GetCurrentProcess().StartTime
                };

                _logger.LogInformation($"   SYSTEM STATUS REQUESTED");
                _logger.LogInformation($"   Connected Clients: {status.ConnectedClients}");
                _logger.LogInformation($"   Total Health Checks: {status.TotalHealthChecks}");
                _logger.LogInformation($"   System Overloaded: {status.SystemOverloaded}");

                return Ok(status);
            }
        }

        /// <summary>
        /// Endpoint برای ریست کردن آمار (برای تست)
        /// </summary>
        [HttpPost("reset")]
        public IActionResult ResetMetrics()
        {
            lock (_lock)
            {
                _connectedClients = 0;
                _totalHealthChecks = 0;
                _systemOverloaded = false;
                _lastHealthCheck = DateTime.MinValue;

                _logger.LogWarning($"   SYSTEM METRICS RESET");
                _logger.LogWarning($"   All counters and states have been reset to default values");
            }

            return Ok(new { Message = "System metrics reset successfully" });
        }
    }
}


