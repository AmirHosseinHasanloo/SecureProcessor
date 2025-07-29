using Grpc.Net.Client;
using Microsoft.AspNetCore.Mvc;
using SecureProcessor.Shared.Extentions;
using SecureProcessor.Shared.Models;
using SecureProcessor.Shared.Protos;
using HealthCheckRequest = SecureProcessor.Shared.Protos.HealthCheckRequest;
using Message = SecureProcessor.Shared.Protos.Message;

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
        public IActionResult HealthCheck([FromBody] HealthCheckRequest request)
        {
            // ✅ اضافه کردن لاگ برای عیب‌یابی
            _logger.LogInformation($"Health check received: Id={request?.Id}, Clients={request?.NumberOfConnectedClients}, Time={request?.SystemTime}");

            // ✅ اعتبارسنجی بهتر
            if (request == null)
            {
                _logger.LogWarning("Health check request is null");
                return BadRequest("Request body is required");
            }

            if (string.IsNullOrEmpty(request.Id))
            {
                _logger.LogWarning("Health check request missing Id");
                return BadRequest("Id is required");
            }

            // ✅ منطق پاسخ
            var response = new Shared.Models.HealthCheckResponse
            {
                IsEnabled = true,
                NumberOfActiveClients = Math.Min(request.NumberOfConnectedClients, 5),
                ExpirationTime = DateTime.UtcNow.AddMinutes(10)
            };

            return Ok(response);
        }

        /// <summary>
        /// Endpoint برای ارسال پیام به Dispatcher از طریق gRPC
        /// </summary>
        [HttpPost("process-request")]
        public async Task<IActionResult> ProcessMessageRequest([FromBody] ProcessRequestModel request)
        {
            _logger.LogInformation($"EXTERNAL PROCESS REQUEST RECEIVED Date : {DateToShamsi.ToShamsi(DateTime.UtcNow)}");
            _logger.LogInformation($"Request ID: {request?.RequestId ?? "NULL"}");

            if (request == null)
            {
                _logger.LogError($"PROCESS REQUEST FAILED: Request is NULL , Date : {DateToShamsi.ToShamsi(DateTime.UtcNow)}");
                return BadRequest("Request body is required");
            }

            if (string.IsNullOrEmpty(request.RequestId))
            {
                _logger.LogWarning("PROCESS REQUEST WARNING: Missing Request ID");
                return BadRequest("RequestId is required");
            }

            if (request.Message == null)
            {
                _logger.LogWarning("PROCESS REQUEST WARNING: Missing Message");
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

                _logger.LogInformation($"SENDING MESSAGE TO DISPATCHER VIA gRPC , Date : {DateToShamsi.ToShamsi(DateTime.UtcNow)}");

                var response = await client.SubmitExternalMessageAsync(grpcRequest);

                _logger.LogInformation($"DISPATCHER RESPONSE RECEIVED");
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
                _logger.LogError(ex, "ERROR IN PROCESS REQUEST");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}