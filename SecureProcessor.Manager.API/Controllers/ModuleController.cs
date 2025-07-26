using Microsoft.AspNetCore.Mvc;
using SecureProcessor.Shared.Models;
using Grpc.Net.Client;
using SecureProcessor.Shared.Protos;
using Message = SecureProcessor.Shared.Protos.Message;
using HealthCheckRequest = SecureProcessor.Shared.Protos.HealthCheckRequest;

namespace SecureProcessor.Manager.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ModuleController : ControllerBase
    {
        private readonly ILogger<ModuleController> _logger;

        public ModuleController(ILogger<ModuleController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Health check endpoint - forwards to Dispatcher via gRPC
        /// </summary>
        [HttpPost("health")]
        public async Task<IActionResult> HealthCheck([FromBody] HealthCheckRequestModel request)
        {
            _logger.LogInformation("🏥 HEALTH CHECK REQUEST RECEIVED");

            if (request == null)
            {
                _logger.LogError("❌ HEALTH CHECK FAILED: Request is NULL");
                return BadRequest("Request body is required");
            }

            if (string.IsNullOrEmpty(request.Id))
            {
                _logger.LogWarning("⚠️ HEALTH CHECK WARNING: Missing Client ID");
                return BadRequest("Id is required");
            }

            try
            {
                // ایجاد کانال gRPC به Dispatcher
                using var channel = GrpcChannel.ForAddress("https://localhost:5001");
                var client = new MessageDispatcherService.MessageDispatcherServiceClient(channel);

                // تبدیل مدل به پیام gRPC
                var grpcRequest = new HealthCheckRequest
                {
                    Id = request.Id,
                    SystemTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    NumberOfConnectedClients = request.NumberOfConnectedClients
                };

                _logger.LogInformation($"🔄 FORWARDING HEALTH CHECK TO DISPATCHER VIA gRPC");

                var response = await client.HealthCheckAsync(grpcRequest);

                var result = new HealthCheckResponseModel
                {
                    IsEnabled = response.IsEnabled,
                    NumberOfActiveClients = response.NumberOfActiveClients,
                    ExpirationTime = DateTime.UtcNow.AddMinutes(10)
                };

                _logger.LogInformation($"✅ HEALTH CHECK RESPONSE: Enabled={result.IsEnabled}, Active={result.NumberOfActiveClients}");

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 ERROR IN HEALTH CHECK");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Endpoint برای ارسال پیام به Dispatcher از طریق gRPC
        /// </summary>
        [HttpPost("process-request")]
        public async Task<IActionResult> ProcessMessageRequest([FromBody] ProcessRequestModel request)
        {
            _logger.LogInformation("📨 EXTERNAL PROCESS REQUEST RECEIVED");
            _logger.LogInformation($"   Request ID: {request?.RequestId ?? "NULL"}");

            if (request == null)
            {
                _logger.LogError("❌ PROCESS REQUEST FAILED: Request is NULL");
                return BadRequest("Request body is required");
            }

            if (string.IsNullOrEmpty(request.RequestId))
            {
                _logger.LogWarning("⚠️ PROCESS REQUEST WARNING: Missing Request ID");
                return BadRequest("RequestId is required");
            }

            if (request.Message == null)
            {
                _logger.LogWarning("⚠️ PROCESS REQUEST WARNING: Missing Message");
                return BadRequest("Message is required");
            }

            try
            {
                // ایجاد کانال gRPC به Dispatcher
                using var channel = GrpcChannel.ForAddress("https://localhost:5001");
                var client = new MessageDispatcherService.MessageDispatcherServiceClient(channel);

                // تبدیل مدل به پیام gRPC
                var grpcRequest = new ExternalMessageRequest
                {
                    RequestId = request.RequestId,
                    RequesterId = request.RequesterId ?? "api-client",
                    RequestTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    Message = new Message
                    {
                        Id = request.Message.Id,
                        Sender = request.Message.Sender ?? "External",
                        Content = request.Message.Content ?? ""
                    }
                };

                _logger.LogInformation($"🔄 SENDING MESSAGE TO DISPATCHER VIA gRPC");

                var response = await client.SubmitExternalMessageAsync(grpcRequest);

                _logger.LogInformation($"✅ DISPATCHER RESPONSE RECEIVED");
                _logger.LogInformation($"   Status: {response.Status}");
                _logger.LogInformation($"   Message: {response.Message}");

                var result = new ProcessResponseModel
                {
                    Status = response.Status,
                    Message = response.Message,
                    ResponseTime = DateTime.UtcNow
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 ERROR IN PROCESS REQUEST");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}